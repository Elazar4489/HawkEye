using FlightDashboardApi.Models;

namespace FlightDashboardApi.Models
{
    public class DashboardDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DashboardName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public double MinLatitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLongitude { get; set; }
        public int TotalFlights { get; set; }
        public List<FlightDocument> Flights { get; set; } = new();
    }
}