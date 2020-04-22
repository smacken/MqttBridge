using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using Polly;

namespace MqttBridgeIotHub
{
    public class IotHubBridgeOptions
    {
        public IMqttClientOptions LocalOptions { get; set; }
        public TopicFilter[] LocalFilters { get; set; }

        public string IotHubConnectionString { get; set; }
        public string IotHubBridgeDeviceName { get; set; } = "MqttBridge";
    }
    public class IotHubBridge
    {
        public DeviceClient DeviceClient { get; set; }
        public IMqttClient LocalClient { get; private set; }

        public IotHubBridgeOptions Options { get; set; }

        public IotHubBridge(IotHubBridgeOptions options)
        {
            var factory = new MqttFactory();
            LocalClient = factory.CreateMqttClient(new MqttNetLogger("LocalClient"));
            DeviceClient = DeviceClient.CreateFromConnectionString(options.IotHubConnectionString, TransportType.Mqtt);
        }

        public async Task<MQTTnet.Client.Connecting.MqttClientAuthenticateResult> ConnectAsync(CancellationToken cancellationToken)
        {
            var cancelToken = cancellationToken != null ? cancellationToken : CancellationToken.None;
            MQTTnet.Client.Connecting.MqttClientAuthenticateResult connectPrimary = null;
            if (!LocalClient.IsConnected)
            {
                LocalClient.UseConnectedHandler(async e =>
                {
                    var primaryFilters = Options.LocalFilters != null && Options.LocalFilters.Any() 
                        ? Options.LocalFilters 
                        : new TopicFilter[] { new TopicFilterBuilder().WithTopic("#").Build() };
                    await LocalClient.SubscribeAsync(primaryFilters);
                });

                connectPrimary = await LocalClient.ConnectAsync(Options.LocalOptions, cancelToken);
                if(connectPrimary.ResultCode != MqttClientConnectResultCode.Success)
                    throw new ArgumentException("LocalOptions, could not connect to primary server.");
            }           

            LocalClient.UseDisconnectedHandler(async e =>
            {
                var connectRetry = Policy
                    .Handle<Exception>()
                    .WaitAndRetryForeverAsync(
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),    
                        (exception, timespan) =>
                        {
                            Console.WriteLine("### DISCONNECTED FROM SERVER ###");     
                        });
                await connectRetry.ExecuteAsync(async () => {
                    var response = await LocalClient.ConnectAsync(Options.LocalOptions, cancelToken);
                    Console.Write($"{response.ResultCode} {response.ReasonString}");
                });
            });

            LocalClient.UseApplicationMessageReceivedHandler(e =>
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(e.ApplicationMessage.Topic)
                    .WithPayload(e.ApplicationMessage.Payload);
                if (e.ApplicationMessage.Retain) message = message.WithRetainFlag();
                switch (e.ApplicationMessage.QualityOfServiceLevel)
                {
                    case MqttQualityOfServiceLevel.ExactlyOnce:
                        message = message.WithExactlyOnceQoS();
                        break;
                    case MqttQualityOfServiceLevel.AtLeastOnce:
                        message = message.WithAtLeastOnceQoS();
                        break;
                    case MqttQualityOfServiceLevel.AtMostOnce:
                        message = message.WithAtMostOnceQoS();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Task.Run(async () => await SendHubEventAsync(e.ClientId, message.Build()), cancelToken);
            });

            await ReceiveHubMessagesAsync().ConfigureAwait(false);
            return connectPrimary;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            var cancelToken = cancellationToken != null ? cancellationToken : CancellationToken.None;
            var disconnectOptions = new MqttClientDisconnectOptions { ReasonString = "Bridge disconnect" };
            await LocalClient.DisconnectAsync(disconnectOptions, cancelToken);
            await DeviceClient.CloseAsync(cancelToken);
        }

        private async Task SendHubEventAsync(string clientId, MqttApplicationMessage message)
        {
            using (var eventMessage = new Message(message.Payload))
            {
                eventMessage.Properties.Add("clientId", clientId);
                eventMessage.Properties.Add("topic",message.Topic);
                
                Console.WriteLine("\t{0}> Sending message: Data: [{2}]", DateTime.Now.ToLocalTime(), message.ConvertPayloadToString());
                await DeviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }
        }

        private async Task ReceiveHubMessagesAsync()
        {
            using (Message receivedMessage = await DeviceClient.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false))
            {
                if (receivedMessage == null)
                {
                    Console.WriteLine("\t{0}> Timed out", DateTime.Now.ToLocalTime());
                    return;
                }
                
                var message = new MqttApplicationMessageBuilder()
                    .WithPayload(receivedMessage.GetBytes());
                string payload = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), payload);
                if (receivedMessage.Properties.ContainsKey("topic"))
                    message.WithTopic(receivedMessage.Properties["topic"]);
                var keyExclude = new string[] { "topic", "clientId" };
                foreach (var prop in receivedMessage.Properties.Where(k => !keyExclude.Contains(k.Key)))
                    message.WithUserProperty(prop.Key, prop.Value);
                await LocalClient.PublishAsync(message.Build(), CancellationToken.None);
                await DeviceClient.CompleteAsync(receivedMessage).ConfigureAwait(false);
            }
        }
    }
}
