﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using Polly;

namespace MqttBridge
{
    public class Bridge
    {
        public IMqttClient PrimaryClient { get; private set; }
        public IMqttClient SecondaryClient { get; private set; }
        public BridgeOptions Options { get; set; }
        public Bridge(BridgeOptions options)
        {
            Options = options;
            var factory = new MqttFactory();
            PrimaryClient = factory.CreateMqttClient(new MqttNetLogger("PrimaryClient"));
            SecondaryClient = factory.CreateMqttClient(new MqttNetLogger("SecondaryClient"));
        }

        public async Task<MQTTnet.Client.Connecting.MqttClientAuthenticateResult> ConnectAsync(CancellationToken cancellationToken)
        {
            var cancelToken = cancellationToken != null ? cancellationToken : CancellationToken.None;
            MqttClientAuthenticateResult connectPrimary = null;
            if (!PrimaryClient.IsConnected)
            {
                PrimaryClient.UseConnectedHandler(async e =>
                {
                    var primaryFilters = Options.PrimaryFilters != null && Options.PrimaryFilters.Any() 
                        ? Options.PrimaryFilters 
                        : new TopicFilter[] { new TopicFilterBuilder().WithTopic("#").Build() };
                    await PrimaryClient.SubscribeAsync(primaryFilters);
                });

                connectPrimary = await PrimaryClient.ConnectAsync(Options.PrimaryOptions, cancelToken);
                if(connectPrimary.ResultCode != MqttClientConnectResultCode.Success)
                    throw new ArgumentException("PrimaryOptions, could not connect to primary server.");
            }
            
            if (!SecondaryClient.IsConnected)
            {
                var connectSecondary = await SecondaryClient.ConnectAsync(Options.SecondaryOptions, cancelToken);
                if(connectSecondary.ResultCode != MqttClientConnectResultCode.Success)
                    throw new ArgumentException("SecondaryOptions, could not connect to secondary server.");
            }
            

            PrimaryClient.UseDisconnectedHandler(async e =>
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
                    var response = await PrimaryClient.ConnectAsync(Options.PrimaryOptions, cancelToken);
                    Console.Write($"{response.ResultCode} {response.ReasonString}");
                });
            });

            SecondaryClient.UseDisconnectedHandler(async e =>
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
                    var response = await SecondaryClient.ConnectAsync(Options.SecondaryOptions, cancelToken);
                    Console.Write($"{response.ResultCode} {response.ReasonString}");
                });
            });

            PrimaryClient.UseApplicationMessageReceivedHandler(e =>
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

                Task.Run(() => SecondaryClient.PublishAsync(message.Build(), cancelToken), cancelToken);
            });

            if (Options.SyncMode)
            {
                SecondaryClient.UseConnectedHandler(async e =>
                {
                    var secondaryFilters = Options.SecondaryFilters != null && Options.SecondaryFilters.Any()
                        ? Options.SecondaryFilters
                        : new TopicFilter[] { new TopicFilterBuilder().WithTopic("#").Build() };
                    await SecondaryClient.SubscribeAsync(secondaryFilters);
                });

                SecondaryClient.UseApplicationMessageReceivedHandler(e =>
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

                    Task.Run(() => PrimaryClient.PublishAsync(message.Build(), cancelToken), cancelToken);
                });
            }
            return connectPrimary;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            var cancelToken = cancellationToken != null ? cancellationToken : CancellationToken.None;
            var disconnectOptions = new MqttClientDisconnectOptions { ReasonString = "Bridge disconnect" };
            await PrimaryClient.DisconnectAsync(disconnectOptions, cancelToken);
            await SecondaryClient.DisconnectAsync(disconnectOptions, cancelToken);
        }
    }
}
