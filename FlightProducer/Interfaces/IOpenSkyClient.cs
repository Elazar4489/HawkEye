using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IOpenSkyClient
{
    Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken);
}