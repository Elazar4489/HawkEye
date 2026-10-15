namespace FlightTracking.Api.DTOs
{
    // Sent by the NotificationWorker to /api/notifications/send-signalr and
    // forwarded as-is to the browser ("ReceiveFlightAlert").
    public class FlightAlertNotificationDto
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Icao24 { get; set; }
        public string? Callsign { get; set; }
        public string? FlightNumber { get; set; }
        public string? ZoneName { get; set; }
        public int Priority { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? BaroAltitude { get; set; }
        public DateTime MatchedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class NotificationHistoryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Icao24 { get; set; }
        public string? Callsign { get; set; }
        public string? ZoneName { get; set; }
        public int Priority { get; set; }
        public string Severity { get; set; } = "Low";
        public string Channel { get; set; } = "DB";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? BaroAltitude { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }

    public static class Severity
    {
        public static string FromPriority(int priority) => priority switch
        {
            1 => "High",
            2 => "Medium",
            _ => "Low"
        };
    }
}
