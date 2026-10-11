
using System.Text.Json;
using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;

    public KafkaFlightProducer(IOptions<KafkaOptions> options)
    {
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

        await _producer.ProduceAsync(
            _options.Topic,
            message,
            cancellationToken);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
