
using Confluent.Kafka;
using FlightMatching.Configuration;

namespace FlightMatching.Data;

public sealed class KafkaFlightConsumer : IDisposable
{
    private readonly IConsumer<string, string> _consumer;

    public KafkaFlightConsumer(KafkaOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(options.InputTopic);
    }

    public ConsumeResult<string, string> Consume(
        CancellationToken cancellationToken)
    {
        return _consumer.Consume(cancellationToken);
    }

    public void Commit(ConsumeResult<string, string> result)
    {
        _consumer.Commit(result);
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}
