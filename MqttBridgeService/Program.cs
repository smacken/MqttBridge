using System;
using Serilog;
using Topshelf;

namespace MqttBridgeService
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile(AppDomain.CurrentDomain.BaseDirectory + "\\logs\\app-{Date}.log")
                .WriteTo.Console()
                .MinimumLevel.Information()
                .CreateLogger();
            
            var host = HostFactory.New(x =>
            {
                x.Service<BridgeService>(s =>
                {
                    s.ConstructUsing(name=> new BridgeService());
                    s.WhenStarted((sc, hostControl) => sc.Start(hostControl));
                    s.WhenStopped((sc, hostControl) => sc.Stop(hostControl));
                });

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1);
                });

                x.OnException(ex =>
                {
                    Log.Error(ex, ex.Message);
                });
                x.UseSerilog();
                x.RunAsLocalSystem();

                x.SetDescription("MqttBridge for Mqtt Broker");
                x.SetDisplayName("MqttBridge");
                x.SetServiceName("MqttBridge");

                x.StartAutomatically();
                x.EnableShutdown();
            });
            
            TopshelfExitCode serviceExitCode;
            try
            {
                serviceExitCode = host.Run();
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, ex.Message);
                return;
            }
            var exitCode = (int) Convert.ChangeType(serviceExitCode, serviceExitCode.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}
