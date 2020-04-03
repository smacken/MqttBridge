using System;
using CommandLine;

namespace MqttBridgeCli
{
    interface IConfigOptions
    {
        [Option("config", SetName = "config")]
        string Config { get; set; }
    }

    interface IServerOptions
    {
        [Option("primary", SetName = "config", HelpText = "Primary Mqtt broker address:port")]
        string Primary { get; set; }

        [Option("secondary", SetName = "config", HelpText = "Secondary Mqtt broker address:port")]
        string Secondary { get; set; }

        [Option("primaryUser", SetName = "config", Required = false)]
        string PrimaryUsername { get; set; }

        [Option("primaryPass", SetName = "config", Required = false)]
        string PrimaryPassword { get; set; }

        [Option("secondaryUser", SetName = "config", Required = false)]
        string SecondaryUsername { get; set; }

        [Option("secondaryPass", SetName = "config", Required = false)]
        string SecondaryPassword { get; set; }
    }

    class Options : IServerOptions, IConfigOptions
    {
        // server options
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string PrimaryUsername { get; set; }
        public string PrimaryPassword { get; set; }
        public string SecondaryUsername { get; set; }
        public string SecondaryPassword { get; set; }

        public string Config { get; set; }

        [Option("sync", Default = false, HelpText = "Synchronize between brokers")]
        public bool Sync { get; set; }

        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option(Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
    }
}
