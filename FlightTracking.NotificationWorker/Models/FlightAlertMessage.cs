namespace FlightTracking.NotificationWorker.Models
{
    public class FlightAlertMessage
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;
    }
}