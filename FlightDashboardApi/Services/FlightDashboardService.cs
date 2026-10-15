using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using FlightDashboardApi.Models;
using MongoDB.Driver;
using Serilog;

namespace FlightDashboardApi.Services
{
    public class FlightDashboardService
    {
        private const double LowAltitudeMeters = 1000;
        private const double RapidDescentMetersPerSecond = -15;
        private const double HighSpeedMetersPerSecond = 250;
        private const int MaxStoredFlights = 2000;

        // The service is scoped; the flag only needs to survive for the process.
        private static bool _indexReady;

        private readonly IMongoDatabase _mongoDatabase;
        private readonly ElasticsearchClient _elasticClient;
        private readonly string _indexName;

        public FlightDashboardService(
            IMongoDatabase mongoDatabase,
            ElasticsearchClient elasticClient,
            IConfiguration configuration)
        {
            _mongoDatabase = mongoDatabase;
            _elasticClient = elasticClient;
            _indexName = configuration["ElasticSettings:IndexName"] ?? "dashboards-index";
        }

        public async Task<DashboardDocument> GenerateAndSaveDashboardAsync(DashboardRequestDto request)
        {
            Log.Information("Filtering flights from MongoDB for area: Lat [{MinLat} - {MaxLat}], Lon [{MinLon} - {MaxLon}]",
                request.MinLatitude, request.MaxLatitude, request.MinLongitude, request.MaxLongitude);

            var collection = _mongoDatabase.GetCollection<FlightDocument>("flights");

            // Only the most recent OpenSky snapshot, so each aircraft is counted once.
            var latest = await collection
                .Find(FilterDefinition<FlightDocument>.Empty)
                .SortByDescending(f => f.RecordedAt)
                .Project(f => f.RecordedAt)
                .FirstOrDefaultAsync();

            var flights = new List<FlightDocument>();

            if (latest != default)
            {
                // שאילתת סינון מרחבית במונגו (Bounding Box)
                var filter = Builders<FlightDocument>.Filter.And(
                    Builders<FlightDocument>.Filter.Eq(f => f.RecordedAt, latest),
                    Builders<FlightDocument>.Filter.Gte(f => f.Latitude, request.MinLatitude),
                    Builders<FlightDocument>.Filter.Lte(f => f.Latitude, request.MaxLatitude),
                    Builders<FlightDocument>.Filter.Gte(f => f.Longitude, request.MinLongitude),
                    Builders<FlightDocument>.Filter.Lte(f => f.Longitude, request.MaxLongitude)
                );

                flights = await collection.Find(filter).ToListAsync();
            }

            Log.Information("Found {Count} flights. Creating and saving dashboard to Elasticsearch...", flights.Count);

            // בניית מסמך הדשבורד לשמירה באלסטיק
            var dashboardDoc = new DashboardDocument
            {
                DashboardName = request.DashboardName,
                SnapshotRecordedAt = latest == default ? null : DateTime.SpecifyKind(latest, DateTimeKind.Utc),
                MinLatitude = request.MinLatitude,
                MaxLatitude = request.MaxLatitude,
                MinLongitude = request.MinLongitude,
                MaxLongitude = request.MaxLongitude,
                TotalFlights = flights.Count,
                Risk = AnalyzeRisk(flights),
                Flights = flights.Take(MaxStoredFlights).ToList()
            };

            // שמירה ב-Elasticsearch
            await EnsureIndexAsync();
            var response = await _elasticClient.IndexAsync(dashboardDoc, idx => idx.Index(_indexName).Id(dashboardDoc.Id));

            if (!response.IsValidResponse)
            {
                Log.Error("Failed to save dashboard to Elasticsearch: {Error}", response.DebugInformation);
                throw new Exception("Failed to save dashboard to Elasticsearch.");
            }

            Log.Information("Dashboard successfully saved with ID: {DashboardId}", dashboardDoc.Id);

            return dashboardDoc;
        }

        public async Task<DashboardDocument?> GetDashboardByIdAsync(string id)
        {
            Log.Information("Retrieving dashboard with ID {Id} from Elasticsearch", id);

            var response = await _elasticClient.GetAsync<DashboardDocument>(id, idx => idx.Index(_indexName));

            if (!response.IsValidResponse || response.Source == null)
            {
                Log.Warning("Dashboard with ID {Id} not found in Elasticsearch", id);
                return null;
            }

            return response.Source;
        }

        // Explicit mapping: dynamic mapping in ES 8.11 rejects the first document when the
        // "flights" array holds 128+ objects ("can't merge a non object mapping [flights]...").
        // Typed fields also make the index usable in Kibana (dates, keywords, numbers).
        private async Task EnsureIndexAsync()
        {
            if (_indexReady) return;

            var exists = await _elasticClient.Indices.ExistsAsync(_indexName);
            if (!exists.Exists)
            {
                var flightProperties = new Properties
                {
                    { "id", new KeywordProperty() },
                    { "icao24", new KeywordProperty() },
                    { "callsign", new KeywordProperty() },
                    { "originCountry", new KeywordProperty() },
                    { "latitude", new DoubleNumberProperty() },
                    { "longitude", new DoubleNumberProperty() },
                    { "baroAltitude", new DoubleNumberProperty() },
                    { "velocity", new DoubleNumberProperty() },
                    { "trueTrack", new DoubleNumberProperty() },
                    { "verticalRate", new DoubleNumberProperty() },
                    { "onGround", new BooleanProperty() },
                    { "recordedAt", new DateProperty() }
                };

                var create = await _elasticClient.Indices.CreateAsync(new CreateIndexRequest(_indexName)
                {
                    Mappings = new TypeMapping
                    {
                        Properties = new Properties
                        {
                            { "id", new KeywordProperty() },
                            { "dashboardName", new KeywordProperty() },
                            { "createdAt", new DateProperty() },
                            { "snapshotRecordedAt", new DateProperty() },
                            { "totalFlights", new IntegerNumberProperty() },
                            { "flights", new ObjectProperty { Properties = flightProperties } }
                        }
                    }
                });

                // Another request may have created it in the meantime - that is fine.
                if (!create.IsValidResponse && !(await _elasticClient.Indices.ExistsAsync(_indexName)).Exists)
                {
                    Log.Error("Failed to create Elasticsearch index {Index}: {Error}", _indexName, create.DebugInformation);
                    throw new Exception("Failed to create Elasticsearch index.");
                }
            }

            _indexReady = true;
        }

        private static RiskSummary AnalyzeRisk(List<FlightDocument> flights)
        {
            var summary = new RiskSummary();
            if (flights.Count == 0) return summary;

            var airborne = flights.Where(f => !f.OnGround).ToList();
            summary.AirborneCount = airborne.Count;
            summary.OnGroundCount = flights.Count - airborne.Count;

            var altitudes = airborne.Where(f => f.BaroAltitude.HasValue).Select(f => f.BaroAltitude!.Value).ToList();
            var velocities = airborne.Where(f => f.Velocity.HasValue).Select(f => f.Velocity!.Value).ToList();
            summary.AverageAltitude = altitudes.Count > 0 ? Math.Round(altitudes.Average(), 0) : 0;
            summary.AverageVelocity = velocities.Count > 0 ? Math.Round(velocities.Average(), 1) : 0;

            foreach (var flight in airborne)
            {
                var reasons = new List<string>();

                if (flight.BaroAltitude is < LowAltitudeMeters)
                {
                    summary.LowAltitudeCount++;
                    reasons.Add("LowAltitude");
                }
                if (flight.VerticalRate is < RapidDescentMetersPerSecond)
                {
                    summary.RapidDescentCount++;
                    reasons.Add("RapidDescent");
                }
                if (flight.Velocity is > HighSpeedMetersPerSecond)
                {
                    summary.HighSpeedCount++;
                    reasons.Add("HighSpeed");
                }

                if (reasons.Count > 0 && summary.RiskFlights.Count < 25)
                {
                    summary.RiskFlights.Add(new RiskFlight
                    {
                        Icao24 = flight.Icao24,
                        Callsign = flight.Callsign?.Trim(),
                        OriginCountry = flight.OriginCountry,
                        BaroAltitude = flight.BaroAltitude,
                        Velocity = flight.Velocity,
                        VerticalRate = flight.VerticalRate,
                        Reasons = reasons
                    });
                }
            }

            // Weighted share of "risky" aircraft among the airborne ones.
            var weighted = summary.LowAltitudeCount * 3 + summary.RapidDescentCount * 2 + summary.HighSpeedCount;
            summary.RiskScore = airborne.Count == 0
                ? 0
                : (int)Math.Min(100, Math.Round(weighted * 100.0 / (airborne.Count * 3)));
            summary.RiskLevel = summary.RiskScore >= 40 ? "High" : summary.RiskScore >= 15 ? "Medium" : "Low";

            summary.CountryBreakdown = flights
                .GroupBy(f => string.IsNullOrWhiteSpace(f.OriginCountry) ? "Unknown" : f.OriginCountry!)
                .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .Take(8)
                .ToList();

            var bands = new (string Name, double Min, double Max)[]
            {
                ("0-1 km", double.MinValue, 1000),
                ("1-3 km", 1000, 3000),
                ("3-6 km", 3000, 6000),
                ("6-9 km", 6000, 9000),
                ("9+ km", 9000, double.MaxValue)
            };
            summary.AltitudeBands = bands
                .Select(b => new NamedCount
                {
                    Name = b.Name,
                    Count = altitudes.Count(a => a >= b.Min && a < b.Max)
                })
                .ToList();

            return summary;
        }
    }
}
