using FlightMatching.Models;


namespace FlightMatching.Data
{
    public interface IFlightConsumer
    {
        Task<FlightState?> Consume(CancellationToken cancellationToken);
    }
}
