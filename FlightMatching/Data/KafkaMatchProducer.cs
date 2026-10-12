
using System.Text.Json;
using Confluent.Kafka;
using FlightMatching.Configuration;
using FlightMatching.Models;

namespace FlightMatching.Data;

public sealed class KafkaMatchProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaMatchProducer(KafkaOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topic = options.OutputTopic;
    }

    public async Task ProduceAsync(
        FlightMatchMessage message,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);

        await _producer.ProduceAsync(
            _topic,
            new Message<string, string>
            {
                Key = $"{message.Icao24}:{message.UserId}",
                Value = json
            },
            cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
