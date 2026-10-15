
namespace FlightMatching.Configuration;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:29092";
    public string InputTopic { get; set; } = "flights";
    public string OutputTopic { get; set; } = "flight-alerts";
    public string GroupId { get; set; } = "flight-matching-group";
}
