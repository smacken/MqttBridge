using System;
using System.Threading;
using System.Threading.Tasks;
using MqttBridge;
using MQTTnet.Client.Connecting;
using Should;
using Xunit;

namespace MqttBridgeTest
{
    public class BridgeTest
    {
        [Fact]
        public async Task Connect_ShouldConnectToBroker()
        {
            var bridge = new Bridge(null);
            var result = await bridge.ConnectAsync(CancellationToken.None);
            result.ResultCode.ShouldEqual(MqttClientConnectResultCode.Success);
        }
    }
}
