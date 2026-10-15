

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; } = 90;

        // Optional OAuth2 client credentials (https://opensky-network.org/my-opensky).
        // Anonymous access works but has a much smaller daily quota.
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string TokenUrl { get; set; } =
            "https://auth.opensky-network.org/auth/realms/opensky-network/protocol/openid-connect/token";
    }
}
