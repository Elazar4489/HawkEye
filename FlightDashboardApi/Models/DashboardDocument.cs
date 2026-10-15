namespace FlightDashboardApi.Models
{
    // One-time dashboard stored in Elasticsearch (index "dashboards-index", viewable in Kibana).
    public class DashboardDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DashboardName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SnapshotRecordedAt { get; set; }
        public double MinLatitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLongitude { get; set; }
        public int TotalFlights { get; set; }
        public RiskSummary Risk { get; set; } = new();
        public List<FlightDocument> Flights { get; set; } = new();
    }

    public class RiskSummary
    {
        public int AirborneCount { get; set; }
        public int OnGroundCount { get; set; }

        // Airborne below 1,000 m
        public int LowAltitudeCount { get; set; }

        // Descending faster than 15 m/s
        public int RapidDescentCount { get; set; }

        // Faster than 250 m/s (~900 km/h)
        public int HighSpeedCount { get; set; }

        public double AverageAltitude { get; set; }
        public double AverageVelocity { get; set; }

        // 0-100, and "Low" / "Medium" / "High"
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; } = "Low";

        public List<NamedCount> CountryBreakdown { get; set; } = new();
        public List<NamedCount> AltitudeBands { get; set; } = new();
        public List<RiskFlight> RiskFlights { get; set; } = new();
    }

    public class NamedCount
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RiskFlight
    {
        public string Icao24 { get; set; } = string.Empty;
        public string? Callsign { get; set; }
        public string? OriginCountry { get; set; }
        public double? BaroAltitude { get; set; }
        public double? Velocity { get; set; }
        public double? VerticalRate { get; set; }
        public List<string> Reasons { get; set; } = new();
    }
}
