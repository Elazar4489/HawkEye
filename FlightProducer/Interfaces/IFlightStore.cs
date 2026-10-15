using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightStore
{
    Task SaveSnapshotAsync(
        IReadOnlyCollection<FlightState> flights,
        DateTime recordedAtUtc,
        CancellationToken cancellationToken);
}
