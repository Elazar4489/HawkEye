using System.ComponentModel.DataAnnotations;

namespace FlightTracking.Api.Models
{
    // Owned (and migrated) by FlightTracking.NotificationWorker; the API only reads it.
    public class NotificationHistoryItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        [MaxLength(16)]
        public string? Icao24 { get; set; }

        [MaxLength(16)]
        public string? Callsign { get; set; }

        [MaxLength(100)]
        public string? ZoneName { get; set; }

        public int Priority { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? BaroAltitude { get; set; }

        [MaxLength(20)]
        public string Channel { get; set; } = "DB";
    }
}
