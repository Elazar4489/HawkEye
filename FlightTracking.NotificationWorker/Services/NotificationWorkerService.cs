using Confluent.Kafka;
using FlightTracking.NotificationWorker.Services;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightTracking.NotificationWorker
{
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ProcessAlertMessage _processAlertMessage;

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            IConsumer<string, string> consumer,
            IOptions<KafkaSettings> kafkaSettings,
            ProcessAlertMessage processAlertMessage)
        {
            _logger = logger;
            _consumer = consumer;
            _kafkaSettings = kafkaSettings.Value;
            _processAlertMessage = processAlertMessage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Consume() is blocking; yield first so the host finishes starting.
            await Task.Yield();

            _logger.LogInformation("NotificationWorkerService is starting, listening to topic: {Topic}", _kafkaSettings.Topic);

            _consumer.Subscribe(_kafkaSettings.Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                        if (consumeResult != null)
                        {
                            _logger.LogInformation("Received alert message from Kafka: {Message} at Key: {Key}",
                                consumeResult.Message.Value, consumeResult.Message.Key);

                            await _processAlertMessage.ProcessAlertMessageAsync(consumeResult.Message.Value, stoppingToken);

                            _consumer.Commit(consumeResult);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error occurred while consuming message from Kafka.");
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                _consumer.Close();
            }
        }
    }
}
