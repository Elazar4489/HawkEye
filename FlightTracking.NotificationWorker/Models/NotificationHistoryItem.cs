using System.ComponentModel.DataAnnotations;

namespace FlightTracking.NotificationWorker.Models
{
    public class NotificationHistoryItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        [MaxLength(16)]
        public string? Icao24 { get; set; }

        [MaxLength(16)]
        public string? Callsign { get; set; }

        [MaxLength(100)]
        public string? ZoneName { get; set; }

        // 1 = High, 2 = Medium, 3 = Low
        public int Priority { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? BaroAltitude { get; set; }

        // Channels used for this alert: "Push", "SignalR", "Push+SignalR" or "DB"
        [MaxLength(20)]
        public string Channel { get; set; } = "DB";
    }
}
