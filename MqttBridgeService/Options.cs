using System;
using System.Collections.Generic;
namespace MqttBridgeService
{
    public class Options
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string PrimaryUsername { get; set; }
        public string PrimaryPassword { get; set; }
        public string SecondaryUsername { get; set; }
        public string SecondaryPassword { get; set; }
        public IEnumerable<string> PrimaryTopicFilters { get; set; } = new List<string>();
        public IEnumerable<string> SecondaryTopicFilters { get; set; } = new List<string>();
        public bool Sync { get; set; }
    }
}
