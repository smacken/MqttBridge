using MQTTnet;
using MQTTnet.Client.Options;

namespace MqttBridge
{
    public class BridgeOptions
    {
        public IMqttClientOptions PrimaryOptions { get; set; }
        public IMqttClientOptions SecondaryOptions { get; set; }

        /// <summary>
        /// Normal bridge is primary -> secondary
        /// Sync is primary <->secondary
        /// </summary>
        public bool SyncMode { get; set; }
        public TopicFilter[] PrimaryFilters { get; set; }
        public TopicFilter[] SecondaryFilters { get; set; }
    }
}
