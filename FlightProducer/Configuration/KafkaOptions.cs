using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
