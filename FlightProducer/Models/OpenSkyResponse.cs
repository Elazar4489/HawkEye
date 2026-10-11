using System.Text.Json;

namespace FlightProducer.Models;

public class OpenSkyResponse
{
    public long Time { get; set; }

    public List<List<JsonElement?>> States { get; set; } = [];
}