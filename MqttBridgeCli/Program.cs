using System;
using System.Threading.Tasks;
using Serilog;
using CommandLine;

namespace MqttBridgeCli
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();
            CommandLine.Parser.Default
                .ParseArguments<Options>(args)
                .MapResult(async options => await BridgeHost.RunOptions(options), _ => Task.FromResult(1));

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            BridgeHost.Disconnect();
        }
    }
}
