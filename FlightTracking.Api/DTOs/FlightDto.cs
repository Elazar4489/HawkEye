namespace FlightTracking.Api.DTOs
{
    public class LiveFlightDto
    {
        public string Icao24 { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string OriginCountry { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? BaroAltitude { get; set; }
        public double? Velocity { get; set; }
        public double? TrueTrack { get; set; }
        public double? VerticalRate { get; set; }
        public bool OnGround { get; set; }
        public long? TimePosition { get; set; }
        public long? LastContact { get; set; }
    }

    public class LiveFlightsResponseDto
    {
        public DateTime? RecordedAtUtc { get; set; }
        public int TotalInSnapshot { get; set; }
        public List<LiveFlightDto> Flights { get; set; } = new();
    }

    public class SystemHealthDto
    {
        public string MySqlStatus { get; set; } = "Offline";
        public string MongoStatus { get; set; } = "Offline";
        public string KafkaStatus { get; set; } = "Offline";
        public DateTime? LastIngestionUtc { get; set; }
        public int LastSnapshotFlights { get; set; }
        public int ActiveSignalRConnections { get; set; }
        public int TotalUsers { get; set; }
        public int TotalZones { get; set; }
    }
}
