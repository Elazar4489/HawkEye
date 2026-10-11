using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; }
    }
}
