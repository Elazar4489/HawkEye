
namespace FlightMatching.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string? OriginCountry { get; set; }

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }

    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }
    public bool OnGround { get; set; }
}
