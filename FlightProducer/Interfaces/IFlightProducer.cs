
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightProducer
{
    Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken);


}
