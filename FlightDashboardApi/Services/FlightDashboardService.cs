using Elastic.Clients.Elasticsearch;
using FlightDashboardApi.Models;
using MongoDB.Driver;
using Serilog;

namespace FlightDashboardApi.Services
{
    public class FlightDashboardService
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ElasticsearchClient _elasticClient;
        private const string IndexName = "dashboards-index";

        public FlightDashboardService(IMongoDatabase mongoDatabase, ElasticsearchClient elasticClient)
        {
            _mongoDatabase = mongoDatabase;
            _elasticClient = elasticClient;
        }

        public async Task<DashboardDocument> GenerateAndSaveDashboardAsync(DashboardRequestDto request)
        {
            Log.Information("Filtering flights from MongoDB for area: Lat [{MinLat} - {MaxLat}], Lon [{MinLon} - {MaxLon}]",
                request.MinLatitude, request.MaxLatitude, request.MinLongitude, request.MaxLongitude);

            var collection = _mongoDatabase.GetCollection<FlightDocument>("flights");

            // שאילתת סינון מרחבית במונגו (Bounding Box)
            var filter = Builders<FlightDocument>.Filter.And(
                Builders<FlightDocument>.Filter.Gte(f => f.Latitude, request.MinLatitude),
                Builders<FlightDocument>.Filter.Lte(f => f.Latitude, request.MaxLatitude),
                Builders<FlightDocument>.Filter.Gte(f => f.Longitude, request.MinLongitude),
                Builders<FlightDocument>.Filter.Lte(f => f.Longitude, request.MaxLongitude)
            );

            var flights = await collection.Find(filter).ToListAsync();

            Log.Information("Found {Count} flights. Creating and saving dashboard to Elasticsearch...", flights.Count);

            // בניית מסמך הדשבורד לשמירה באלסטיק
            var dashboardDoc = new DashboardDocument
            {
                DashboardName = request.DashboardName,
                MinLatitude = request.MinLatitude,
                MaxLatitude = request.MaxLatitude,
                MinLongitude = request.MinLongitude,
                MaxLongitude = request.MaxLongitude,
                TotalFlights = flights.Count,
                Flights = flights
            };

            // שמירה ב-Elasticsearch
            var response = await _elasticClient.IndexAsync(dashboardDoc, idx => idx.Index(IndexName));

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

            var response = await _elasticClient.GetAsync<DashboardDocument>(id, idx => idx.Index(IndexName));

            if (!response.IsValidResponse || response.Source == null)
            {
                Log.Warning("Dashboard with ID {Id} not found in Elasticsearch", id);
                return null;
            }

            return response.Source;
        }
    }
}