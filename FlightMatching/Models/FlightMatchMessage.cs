
namespace FlightMatching.Models;

public class FlightMatchMessage
{
    public int UserId { get; set; }
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public DateTime MatchedAtUtc { get; set; }
}
