namespace FlightDashboardApi.Models
{
    public class DashboardRequestDto
    {
        public double MinLatitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLongitude { get; set; }
        public string DashboardName { get; set; } = "Default Dashboard";
    }
}
