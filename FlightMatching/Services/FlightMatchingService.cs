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

    private async Task<int> ProcessFlightAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var zones = await _matcher.FindMatchingZonesAsync(
            flight,
            cancellationToken);

        // One alert per user: the highest priority zone (lowest number) wins.
        var bestZonePerUser = zones
            .GroupBy(zone => zone.UserId)
            .Select(group => group.First())
            .ToList();

        foreach (var zone in bestZonePerUser)
        {
            var key = (flight.Icao24, zone.UserId);
            var now = DateTime.UtcNow;

            if (_lastNotifications.TryGetValue(key, out var lastSent) &&
                now - lastSent < _cooldown)
            {
                Console.WriteLine(
                    $"Skipping notification for {flight.Icao24}, " +
                    $"user {zone.UserId}: cooldown active.");

                continue;
            }

            var flightNumber = string.IsNullOrWhiteSpace(flight.Callsign)
                ? flight.Icao24
                : flight.Callsign.Trim();

            var zoneName = string.IsNullOrWhiteSpace(zone.ZoneName)
                ? $"#{zone.ZoneId}"
                : zone.ZoneName;

            var message = new FlightMatchMessage
            {
                UserId = zone.UserId,
                Icao24 = flight.Icao24,
                Callsign = flight.Callsign,
                FlightNumber = flightNumber,
                Message = $"Flight {flightNumber} ({flight.Icao24}) entered zone \"{zoneName}\"",
                ZoneId = zone.ZoneId,
                ZoneName = zone.ZoneName,
                Priority = zone.Priority,
                OriginCountry = flight.OriginCountry,
                Longitude = flight.Longitude!.Value,
                Latitude = flight.Latitude!.Value,
                BaroAltitude = flight.BaroAltitude,
                Velocity = flight.Velocity,
                MatchedAtUtc = now
            };

            await _producer.ProduceAsync(message, cancellationToken);

            _lastNotifications[key] = now;
        }

        PruneCooldowns();

        return bestZonePerUser.Count;
    }

    // Keeps the in-memory cooldown table from growing without bound.
    private void PruneCooldowns()
    {
        if (_lastNotifications.Count < 10_000)
            return;

        var threshold = DateTime.UtcNow - _cooldown;

        foreach (var expired in _lastNotifications
                     .Where(entry => entry.Value < threshold)
                     .Select(entry => entry.Key)
                     .ToList())
        {
            _lastNotifications.Remove(expired);
        }
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
            catch (ConsumeException ex)
            {
                Console.Error.WriteLine($"Kafka consume error: {ex.Error.Reason}");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
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

            try
            {
                var matchedUsers = await ProcessFlightAsync(flight, cancellationToken);

                if (matchedUsers > 0)
                {
                    Console.WriteLine(
                        $"Flight {flight.Icao24}: matched {matchedUsers} user(s).");
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A bad message or a transient DB/Kafka error must not crash the
                // service and replay the same message forever. Positions are
                // refreshed every poll, so skipping one is safe.
                Console.Error.WriteLine(
                    $"Failed to process flight {flight.Icao24}: {ex.Message}");
            }

            _consumer.Commit(result);
        }

        Console.WriteLine("FlightMatching stopped.");
    }
}