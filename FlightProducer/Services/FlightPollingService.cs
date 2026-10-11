
using FlightProducer.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly ILogger<FlightPollingService> _logger;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        ILogger<FlightPollingService> logger)
    {
        _openSkyClient = openSkyClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            var flights = await _openSkyClient.GetFlightsAsync(
                stoppingToken);

            _logger.LogInformation(
                "Received {FlightCount} flights from OpenSky",
                flights.Count);

            foreach (var flight in flights.Take(5))
            {
                _logger.LogInformation(
                    "ICAO: {Icao24}, Callsign: {Callsign}, Country: {Country}",
                    flight.Icao24,
                    flight.Callsign,
                    flight.OriginCountry);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // כיבוי יזום של האפליקציה
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve flights from OpenSky");
        }
    }
}
