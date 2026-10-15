namespace FlightTracking.NotificationWorker.Models
{
    // Produced by FlightMatching (FlightMatchMessage) on the "flight-alerts" topic.
    public class FlightAlertMessage
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;

        public string? Icao24 { get; set; }
        public string? Callsign { get; set; }
        public int ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public int Priority { get; set; }
        public string? OriginCountry { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? BaroAltitude { get; set; }
        public double? Velocity { get; set; }
        public DateTime MatchedAtUtc { get; set; }
    }
}
