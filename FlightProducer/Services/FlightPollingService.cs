using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flights = await _openSkyClient.GetFlightsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Received {FlightCount} flights from OpenSky",
                    flights.Count);

                var sentCount = 0;

                foreach (var flight in flights)
                {
                    await _flightProducer.ProduceAsync(
                        flight,
                        stoppingToken);

                    sentCount++;
                }

                _logger.LogInformation(
                    "Successfully sent {SentCount} flights to Kafka",
                    sentCount);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed during flight polling or Kafka publishing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}