# MQTT Bridge

Bridge two MQTT Brokers to pass topic messages between them. Acts as a client on both brokers
rather than broker-broker bridge ala Mosquitto.

The idea is to bridge between physical-physical brokers or physical-cloud brokers.

Bridge can be one-way (primary > secondary) or two-way with sync between brokers.

Topics can be filtered for each broker for messages bridged.

## Getting Started

1. Run the app by entering the following command in the command shell:

   ```console
    dotnet run -- --config=config.json
   ```

2. CLI

Can configure cli via config file in yaml/json. Or by passing values directly

```bash
MqttBridge.exe --config=config.json
```

```bash
MqttBridge.exe --primary=localhost:1883 --secondary=localhost:1884
```

3. Library

Bridge can be configured with primary & secondary brokers.

```c#
   var primaryOptions = new MqttClientOptionsBuilder()
                .WithClientId("Primary")
                .WithTcpServer("localhost", 1883)
                .WithCleanSession();
   var bridgeOptions = new BridgeOptions
   {
         PrimaryOptions = primaryOptions.Build(),
         SecondaryOptions = secondaryOptions.Build(),
         PrimaryFilters = new TopicFilter[] {new TopicFilterBuilder().WithTopic("primary/topic").Build()}
         SecondaryFilters = new TopicFilter[] {new TopicFilterBuilder().WithTopic("secondary/topic").Build()}
         SyncMode = true
   };
   var bridge = new Bridge(bridgeOptions);
   await bridge.ConnectAsync(CancellationToken.None); 
```

### Prerequisites

Install the following:

- [.NET Core](https://dotnet.microsoft.com/download).

### Installing

MqttBridgeCli.exe from releases

Nuget MqttBridge from packages

## Running the tests

xUnit testing

```bash
dotnet test
```


### Break down into end to end tests

Testing templates being used

```
dotnet test
```

### And coding style tests

Editor.config

```
Editor.config
```

## Deployment

```
dotnet publish
```

## Built With

* [MqttNet](https://github.com/migueldeicaza/gui.cs) - MqttNet

## Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/PurpleBooth/b24679402957c63ec426) for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

* **Scott Mackenzie** - *Initial work* - [Smacktech](https://github.com/smacken)

See also the list of [contributors](https://github.com/smacken/templated/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* mmm

