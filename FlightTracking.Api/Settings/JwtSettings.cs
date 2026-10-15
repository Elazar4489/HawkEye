namespace FlightTracking.Api.Settings
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "HawkEye";
        public string Audience { get; set; } = "HawkEye.Clients";
        public int ExpirationHours { get; set; } = 12;
    }
}
