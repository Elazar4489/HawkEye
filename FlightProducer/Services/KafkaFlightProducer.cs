using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFlightProducer> _logger;

    private const int MaxRetries = 2;
    private const int InitialRetryDelayMs = 1000;

    public KafkaFlightProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var tries = 0;
        var delay = InitialRetryDelayMs;

        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

        while (tries <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _producer.ProduceAsync(
                    _options.Topic,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Delivered flight {Icao24} to {Topic}, partition {Partition}, offset {Offset}",
                    flight.Icao24,
                    result.Topic,
                    result.Partition,
                    result.Offset);

                return;
            }
            catch (ProduceException<string, string> ex)
            {
                if (tries == MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send flight {Icao24} to Kafka after {Attempts} attempts",
                        flight.Icao24,
                        tries + 1);

                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to send flight {Icao24}. Attempt {Attempt} failed. Retrying in {DelayMs} ms",
                    flight.Icao24,
                    tries + 1,
                    delay);

                await Task.Delay(delay, cancellationToken);

                delay *= 2;
                tries++;
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish flight {flight.Icao24} after {MaxRetries + 1} attempts.");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}