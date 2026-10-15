using System.Diagnostics;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly IFlightStore _flightStore;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        IFlightStore flightStore,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _flightStore = flightStore;
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

                // Raw data goes to MongoDB (dashboards / live map); a Mongo failure
                // must not stop the Kafka stream that drives the alerts.
                try
                {
                    await _flightStore.SaveSnapshotAsync(
                        flights,
                        DateTime.UtcNow,
                        stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to save flight snapshot to MongoDB");
                }

                const int batchSize = 500;
                var sentCount = 0;
                var stopwatch = Stopwatch.StartNew();
                var failedCount = 0;

                for (int i = 0; i < flights.Count; i += batchSize)
                {
                    var batch = flights
                        .Skip(i)
                        .Take(batchSize)
                        .Select(flight =>
                            TryProduceAsync(flight, stoppingToken))
                        .ToArray();

                    var results = await Task.WhenAll(batch);

                    sentCount += results.Count(success => success);
                    failedCount += results.Count(success => !success);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Kafka publishing completed. Sent: {SentCount}, Failed: {FailedCount}, Duration: {ElapsedSeconds:F2}s, Rate: {MessagesPerSecond:F0} messages/sec",
                    sentCount,
                    failedCount,
                    stopwatch.Elapsed.TotalSeconds,
                    stopwatch.Elapsed.TotalSeconds > 0
                        ? sentCount / stopwatch.Elapsed.TotalSeconds
                        : 0);
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


    private async Task<bool> TryProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        try
        {
            await _flightProducer.ProduceAsync(
                flight,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish flight {Icao24}",
                flight.Icao24);

            return false;
        }
    }
}