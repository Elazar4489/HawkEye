using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FlightProducer.Services;

public class MongoFlightStore : IFlightStore
{
    private readonly IMongoCollection<FlightSnapshotDocument> _collection;
    private readonly ILogger<MongoFlightStore> _logger;
    private readonly MongoOptions _options;
    private bool _indexesCreated;

    public MongoFlightStore(
        IOptions<MongoOptions> options,
        ILogger<MongoFlightStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        var client = new MongoClient(_options.ConnectionString);
        _collection = client
            .GetDatabase(_options.DatabaseName)
            .GetCollection<FlightSnapshotDocument>(_options.CollectionName);
    }

    public async Task SaveSnapshotAsync(
        IReadOnlyCollection<FlightState> flights,
        DateTime recordedAtUtc,
        CancellationToken cancellationToken)
    {
        if (flights.Count == 0)
            return;

        await EnsureIndexesAsync(cancellationToken);

        var documents = flights.Select(flight => new FlightSnapshotDocument
        {
            Icao24 = flight.Icao24,
            Callsign = flight.Callsign,
            OriginCountry = flight.OriginCountry,
            TimePosition = flight.TimePosition,
            LastContact = flight.LastContact,
            Longitude = flight.Longitude,
            Latitude = flight.Latitude,
            BaroAltitude = flight.BaroAltitude,
            OnGround = flight.OnGround,
            Velocity = flight.Velocity,
            TrueTrack = flight.TrueTrack,
            VerticalRate = flight.VerticalRate,
            RecordedAt = recordedAtUtc
        });

        await _collection.InsertManyAsync(
            documents,
            new InsertManyOptions { IsOrdered = false },
            cancellationToken);

        _logger.LogInformation(
            "Saved {FlightCount} flights to MongoDB collection {Collection}",
            flights.Count,
            _options.CollectionName);
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesCreated)
            return;

        var keys = Builders<FlightSnapshotDocument>.IndexKeys;

        await _collection.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<FlightSnapshotDocument>(
                    keys.Ascending(f => f.RecordedAt),
                    new CreateIndexOptions
                    {
                        Name = "recorded_at_ttl",
                        ExpireAfter = TimeSpan.FromHours(_options.RetentionHours)
                    }),
                new CreateIndexModel<FlightSnapshotDocument>(
                    keys.Ascending(f => f.Icao24),
                    new CreateIndexOptions { Name = "icao24" })
            },
            cancellationToken);

        _indexesCreated = true;
    }
}
