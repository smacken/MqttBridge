﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using CommandLine;
using MqttBridge;
using MQTTnet;
using MQTTnet.Client.Options;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }
        
        static void RunOptions(Options opts)
        {
            if (!string.IsNullOrEmpty(opts.Config))
                opts = opts.Config.EndsWith(".yaml") ? ReadYaml(opts.Config) : ReadJson(opts.Config);
            
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
                .WithClientId("Primary")
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
            var bridge = new Bridge(bridgeOptions);
            bridge.ConnectAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter();
        }

        static void HandleParseError(IEnumerable<Error> errors) => Log.Error(string.Join(" ", errors.Select(e => e.ToString())));

        private static Options ReadJson(string filePath)
        {
            Options config = null;
            if (!File.Exists(filePath)) throw new ArgumentException("config file doesnt exist");
            using var r = new StreamReader(filePath);
            var json = r.ReadToEnd();
            config = JsonConvert.DeserializeObject<Options>(json);
            return config;
        }

        private static Options ReadYaml(string filePath)
        {
            if (!File.Exists(filePath)) throw new ArgumentException("registry file doesnt exist");
            using var stream = new StreamReader(filePath);
            var text = stream.ReadToEnd();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var result = deserializer.Deserialize<Options>(text);
            return result;
        }
    }
}
