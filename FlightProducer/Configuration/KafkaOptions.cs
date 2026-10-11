

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
