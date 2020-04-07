using System;
using System.IO;
using System.Linq;
using System.Threading;
using MqttBridge;
using MQTTnet;
using MQTTnet.Client.Options;
using Newtonsoft.Json;
using Serilog;
using Topshelf;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MqttBridgeService
{
    public class BridgeService : ServiceControl
    {
        public Bridge Bridge { get; set; }

        public BridgeService()
        {
            BridgeOptions bridgeOptions = Configure();
            Bridge = new Bridge(bridgeOptions);
        }

        private static BridgeOptions Configure()
        {
            var opts = ReadJson("config.json");
            var primaryPath = opts.Primary.Split(":").First();
            var primaryPortText = opts.Primary.Split(":").Last();
            var primaryParse = int.TryParse(primaryPortText, out var primaryPort);

            var secondaryPath = opts.Secondary.Split(":").First();
            var secondaryPortText = opts.Secondary.Split(":").Last();
            var secondaryParse = int.TryParse(secondaryPortText, out var secondaryPort);
            var primaryOptions = new MqttClientOptionsBuilder()
                .WithClientId("Primary")
                .WithTcpServer(primaryPath, primaryPort)
                .WithCleanSession();
            if (opts.PrimaryUsername != null && opts.PrimaryPassword != null)
                primaryOptions = primaryOptions.WithCredentials(opts.PrimaryUsername, opts.PrimaryPassword);

            var secondaryOptions = new MqttClientOptionsBuilder()
                .WithClientId("Secondary")
                .WithTcpServer(secondaryPath, secondaryPort)
                .WithCleanSession();
            if (opts.SecondaryUsername != null && opts.SecondaryPassword != null)
                secondaryOptions = secondaryOptions.WithCredentials(opts.SecondaryUsername, opts.SecondaryPassword);

            var bridgeOptions = new BridgeOptions
            {
                PrimaryOptions = primaryOptions.Build(),
                SecondaryOptions = secondaryOptions.Build(),
                SyncMode = opts.Sync
            };

            if (opts.PrimaryTopicFilters.Any())
            {
                var primaryFilters = opts.PrimaryTopicFilters
                    .Select(f => new TopicFilterBuilder().WithTopic(f).Build())
                    .ToArray();
                bridgeOptions.PrimaryFilters = primaryFilters;
            }
            if (opts.SecondaryTopicFilters.Any())
            {
                var secondaryFilters = opts.SecondaryTopicFilters
                    .Select(f => new TopicFilterBuilder().WithTopic(f).Build())
                    .ToArray();
                bridgeOptions.SecondaryFilters = secondaryFilters;
            }
            return bridgeOptions;
        }

        public bool Start(HostControl hostControl)
        {
            Log.Information("Starting");
            try
            {
                Bridge.ConnectAsync(CancellationToken.None).ConfigureAwait(false);   
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.Message);
                return false;
            }
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Log.Information("Stopping");
            Bridge.DisconnectAsync(CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter();
            return true;
        }

        private static Options ReadJson(string filePath)
        {
            Options config = null;
            if (!File.Exists(filePath)) throw new ArgumentException("config file doesnt exist");
            using var r = new StreamReader(filePath);
            var json = r.ReadToEnd();
            config = JsonConvert.DeserializeObject<Options>(json);
            return config;
        }
    }
}
