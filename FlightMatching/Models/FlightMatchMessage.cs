
namespace FlightMatching.Models;

// Published to the "flight-alerts" topic and consumed by
// FlightTracking.NotificationWorker (FlightAlertMessage has the same shape).
public class FlightMatchMessage
{
    public int UserId { get; set; }
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }

    // Human readable text and display id used by the notification channels.
    public string Message { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;

    public int ZoneId { get; set; }
    public string? ZoneName { get; set; }

    // 1 = High, 2 = Medium, 3 = Low
    public int Priority { get; set; }

    public string? OriginCountry { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }

    public DateTime MatchedAtUtc { get; set; }
}
