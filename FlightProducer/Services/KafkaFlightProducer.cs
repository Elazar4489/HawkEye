
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
    public KafkaFlightProducer(IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config)
            .Build();
    }

    public async Task ProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

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

    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
