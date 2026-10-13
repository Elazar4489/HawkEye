using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using FlightMatching.Data;
using FlightMatching.Models;

namespace FlightMatching.Services;

public sealed class FlightMatchingService
{
    private readonly KafkaFlightConsumer _consumer;
    private readonly KafkaMatchProducer _producer;
    private readonly MySqlFlightMatcher _matcher;
    private readonly TimeSpan _cooldown;
    private readonly Dictionary<(string Icao24, int UserId), DateTime> _lastNotifications = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public FlightMatchingService(
        KafkaFlightConsumer consumer,
        KafkaMatchProducer producer,
        MySqlFlightMatcher matcher,
        TimeSpan cooldown)
    {
        _consumer = consumer;
        _producer = producer;
        _matcher = matcher;
        _cooldown = cooldown;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("FlightMatching started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string> result;

            try
            {
                result = _consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            FlightState? flight;

            try
            {
                flight = JsonSerializer.Deserialize<FlightState>(
                    result.Message.Value,
                    JsonOptions);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Invalid JSON: {ex.Message}");
                _consumer.Commit(result);
                continue;
            }

            if (flight is null ||
                string.IsNullOrWhiteSpace(flight.Icao24) ||
                flight.Longitude is null ||
                flight.Latitude is null)
            {
                Console.WriteLine("Skipping invalid flight message.");
                _consumer.Commit(result);
                continue;
            }

            var userIds = await _matcher.FindMatchingUserIdsAsync(
                flight,
                cancellationToken);

            foreach (var userId in userIds)
            {
                var key = (flight.Icao24, userId);
                var now = DateTime.UtcNow;

                if (_lastNotifications.TryGetValue(key, out var lastSent) &&
                    now - lastSent < _cooldown)
                {
                    Console.WriteLine(
                        $"Skipping notification for {flight.Icao24}, " +
                        $"user {userId}: cooldown active.");

                    continue;
                }

                var message = new FlightMatchMessage
                {
                    UserId = userId,
                    Icao24 = flight.Icao24,
                    Callsign = flight.Callsign,
                    Longitude = flight.Longitude.Value,
                    Latitude = flight.Latitude.Value,
                    MatchedAtUtc = now
                };

                await _producer.ProduceAsync(message, cancellationToken);

                _lastNotifications[key] = now;
            }

            _consumer.Commit(result);

            Console.WriteLine(
                $"Flight {flight.Icao24}: matched {userIds.Count} user(s).");
        }

        Console.WriteLine("FlightMatching stopped.");
    }
}