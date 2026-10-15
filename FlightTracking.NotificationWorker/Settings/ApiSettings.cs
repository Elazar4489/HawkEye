namespace FlightTracking.NotificationWorker.Settings
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;

        // Shared secret checked by FlightTracking.Api on /api/notifications/send-signalr
        public string InternalApiKey { get; set; } = string.Empty;
    }

    public class FirebaseSettings
    {
        // Path to a Firebase service-account JSON. Push is skipped when empty / missing.
        public string? CredentialsPath { get; set; }
    }
}
