# DataCore.Adapter

Contains base classes and utility classes for implementing an [IAdapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs).

Extend from [AdapterBase](./AdapterBase.cs) for easy implementation or [AdapterBase&lt;T&gt;](./AdapterBaseT.cs) if you need to supply configurable options to your adapter.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter).


# Creating an Adapter

Extend [AdapterBase&lt;T&gt;](./AdapterBaseT.cs) or [AdapterBase](./AdapterBase.cs) to inherit a base adapter implementation. In both cases, you must implement the `StartAsync` and `StopAsync` methods at a bare minimum.


## Working with IAsyncEnumerable<T>

Adapter features make extensive use of the `IAsyncEnumerable<T>` type, to allow query results to be streamed back to the caller asynchronously. For .NET Framework and .NET Standard 2.0 targets, the [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) NuGet package is used to define the type.

> NOTE: In order to produce `IAsyncEnumerable<T>` instances from iterator methods, or to consume `IAsyncEnumerator<T>` instances using `await foreach` loops, your project must use C# 8.0 or higher.

In most cases, it is advisable to declare a feature method using the `async` keyword, and to use `yield return` statements to emit values as they occur. For example:

```csharp
public class MyAdapter : AdapterBase, IReadSnapshotTagValues {

    // -- other code removed for brevity --

    async IAsyncEnumerable<ChannelReader<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
        IAdapterCallContext context, 
        ReadSnapshotTagValuesRequest request, 
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        ValidateInvocation(context, request);

        // CreateCancellationTokenSource is defined on AdapterBase<TOptions>, and is used to 
        // create CancellationTokenSource instances that will cancel when the adapter is stopped, 
        // in addition to when any cancellation tokens passed to the method are cancelled.
        using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
            await foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token).ConfigureAwait(false)) {
                yield return item;
            }
        }
    }

    private IAsyncEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
        ...
    }

}
```

If your implementation runs synchronously (e.g. if the return values are held in an in-memory collection), you can use `Task.Yield` to make the implementation asynchronous:

```csharp
public class MyAdapter : AdapterBase, IReadSnapshotTagValues {

    // -- other code removed for brevity --

    async IAsyncEnumerable<ChannelReader<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
        IAdapterCallContext context, 
        ReadSnapshotTagValuesRequest request, 
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        ValidateInvocation(context, request);

        await Task.Yield();

        using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
            foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token)) {
                yield return item;
            }
        }
    }

    private IEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
        ...
    }

}
```


# Implementing Features

Standard adapter features are defined as interfaces in the [DataCore.Adapter.Abstractions](/src/DataCore.Adapter.Abstractions) project. Any features that are implemented directly on your adapter class will be added to the adapter's feature collection automatically. If you delegate a feature implementation to another class (for example, by using the [SnapshotTagValuePush](./RealTimeData/SnapshotTagValuePush.cs) class to add [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) functionality to your adapter), you must register this feature yourself, by calling the `AddFeature` or `AddFeatures` method inherited from `AdapterBase`.


## IHealthCheck

Both `AdapterBase` and `AdapterBase<T>` add out-of-the-box support for the [IHealthCheck](/src/DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs) feature. To customise the health checks that are performed, you can override the `CheckHealthAsync` method.

Whenever the health status of your adapter changes (e.g. you become disconnected from an external service that the adapter relies on), you should call the `OnHealthStatusChanged` method from your implementation. This will recompute the overall health status of the adapter and push the update to any subscribers to the `IHealthCheck` feature.


## IEventMessagePush / IEventMessagePushWIthTopics

To add the [IEventMessagePush](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePush.cs) and/or [IEventMessagePushWithTopics](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePushWithTopics.cs) features to your adapter, you can add or extend the [EventMessagePush](./Events/EventMessagePush.cs) and [EventMessagePushWithTopics](./Events/EventMessagePushWithTopics.cs) classes respectively. To push values to subscribers, call the `ValueReceived` method on the feature.

If your source supports its own subscription mechanism, you can extend the `EventMessagePush` and `EventMessagePushWithTopics` classes to interface with it in the appropriate extension points.


## ISnapshotTagValuePush

To add the [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) feature to your adapter, you can use the [SnapshotTagValuePush](./RealTimeData/SnapshotTagValuePush.cs) or [PollingSnapshotTagValuePush](./RealTimeData/PollingSnapshotTagValuePush.cs) classes. The latter can be used when the underlying source does not support a subscription mechanism of its own, and allows subscribers to your adapter to receive real-time value changes at an update rate of your choosing, by polling the underlying source for values on a periodic basis. To push values to subscribers, call the `ValueReceived` method on the feature.

If your source supports its own subscription mechanism, you can extend the `SnapshotTagValuePush` class to interface with it in the appropriate extension points.


## Historical Data Queries 

If your underlying source does not support aggregated, values-at-times, or plot data queries (implemented via the [IReadProcessedTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs), [IReadTagValuesAtTimes](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs), and [IReadPlotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) respectively), you can use the [ReadHistoricalTagValues](./RealTimeData/ReadHistoricalTagValues.cs) class to provide these capabilities, as long as you can provide it with the ability to resolve tag names, and to request raw tag values.

If your source implements some of these capabilities but not others, you can use the classes in the `DataCore.Adapter.RealTimeData.Utilities` namespace to assist with the implementation of the missing functionality if desired.

> Note that using `ReadHistoricalTagValues` or the associated utility classes will almost certainly perform worse than a native implementation; native implementations are always encouraged where available.


## Extension Features

Extension features must inherit from [IAdapterExtensionFeature](/src/DataCore.Adapter.Abstractions/Extensions/IAdapterExtensionFeature.cs), and must be annotated with an [ExtensionFeatureAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureAttribute.cs), which identifies the URI for the extension, as well as additional properties such as the display name and description.

The `IAdapterExtensionFeature` interface defines methods for retrieving a descriptor for the extension, and a list of available operations. Extension operations are called via the `Invoke`, `Stream`, or `DuplexStream` methods defined on `IAdapterExtensionFeature`. 

In all cases, the methods take an `IAdapterCallContext` parameter that allows the operation to identify the caller (and apply appropriate authorisation if required), a URI that is used to identify the extension feature operation that is being called, an input parameter, and a `CancellationToken`.  On the `Invoke` and `Stream` methods, the input parameter is a JSON-encoded input value; on the `DuplexStream` method the input parameter is a `ChannelReader<T>` that provides a stream of JSON-encoded input values. The return value on the `Invoke` method is a JSON-encoded output value, and on the `Stream` and `DuplexStream` methods the return value is a `ChannelReader<T>` that provides a stream of JSON-encoded output values.

The [AdapterExtensionFeature](./Extensions/AdapterExtensionFeature.cs) class is a base class for simplifying the implementation of extension features, which provides a number of `BindInvoke`, `BindStream`, and `BindDuplexStream` methods to automatically generate operation descriptors for the extension feature, and to automatically invoke the bound method (and perform deserialization of inputs from/serialization of outputs to JSON) when a call is made to the extension's `Invoke`, `Stream`, or `DuplexStream` methods.

For example, the full implementation of a "ping pong" extension, that responds to every `PingMessage` it receives with an equivalent `PongMessage` might look like this:

```csharp
[ExtensionFeature(
    "example/ping-pong/",
    Name = "Ping Pong",
    Description = "Responds to every ping message with a pong message"
)]
public class PingPongExtension : AdapterExtensionFeature {

    public PingPongExtension(
        IBackgroundTaskService backgroundTaskService, 
        IEnumerable<IObjectEncoder> encoders
    ) : base(backgroundTaskService, encoders) {
        BindInvoke<PingPongExtension, PingMessage, PongMessage>(PingInvoke);
        BindStream<PingPongExtension, PingMessage, PongMessage>(PingStream);
        BindDuplexStream<PingPongExtension, PingMessage, PongMessage>(PingDuplexStream);
    }


    [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingInvokeDescriptor))]
    public Task<PongMessage> PingInvoke(
        IAdapterCallContext context, 
        PingMessage ping, 
        CancellationToken cancellationToken
    ) {
        if (ping == null) {
            throw new ArgumentNullException(nameof(ping));
        }

        return Task.FromResult(new PongMessage() {
            CorrelationId = ping.CorrelationId
        });
    }


    [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingStreamDescriptor))]
    public async IAsyncEnumerable<PongMessage> PingStream(
        IAdapterCallContext context,
        PingMessage ping,
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        if (ping == null) {
            throw new ArgumentNullException(nameof(ping));
        }

        await Task.Yield();
        yield return new PongMessage() {
            CorrelationId = ping.CorrelationId
        };
    }


    [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingDuplexStreamDescriptor))]
    public async IAsyncEnumerable<PongMessage> PingDuplexStream(
        IAdapterCallContext context,
        IAsyncEnumerable<PingMessage> channel,
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        if (channel == null) {
            throw new ArgumentNullException(nameof(channel));
        }

        await foreach(var ping in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
            if (ping == null) {
                continue;
            }

            yield return new PongMessage() {
                CorrelationId = ping.CorrelationId
            };
        }
    }


    internal static ExtensionFeatureOperationDescriptorPartial GetPingInvokeDescriptor() {
        return new ExtensionFeatureOperationDescriptorPartial() {
            Name = "Ping",
            Description = "Returns a pong message that matches the correlation ID of the specified ping message",
            Inputs = new [] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    VariantType = VariantType.ExtensionObject,
                    TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                    Description = "The ping message"
                }
            },
            Outputs = new [] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    VariantType = VariantType.ExtensionObject,
                    TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                    Description = "The resulting pong message"
                }
            }
        };
    }


    internal static ExtensionFeatureOperationDescriptorPartial GetPingStreamDescriptor() {
        return new ExtensionFeatureOperationDescriptorPartial() {
            Name = "Ping",
            Description = "Returns a pong message every second that matches the correlation ID of the specified ping message",
            Inputs = new[] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    VariantType = VariantType.ExtensionObject,
                    TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                    Description = "The ping message"
                }
            },
            Outputs = new[] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    VariantType = VariantType.ExtensionObject,
                    TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                    Description = "The resulting pong message"
                }
            }
        };
    }


    internal static ExtensionFeatureOperationDescriptorPartial GetPingDuplexStreamDescriptor() {
        return new ExtensionFeatureOperationDescriptorPartial() {
            Name = "Ping",
            Description = "Returns a pong message every time a ping message is received",
            Inputs = new[] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    VariantType = VariantType.ExtensionObject,
                    TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                    Description = "The ping message"
                }
            },
            Outputs = new[] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    VariantType = VariantType.ExtensionObject,
                    TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                    Description = "The resulting pong message"
                }
            }
        };
    }

}


[ExtensionFeatureDataType(typeof(PingPongExtension), "ping-message")]
public class PingMessage {
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}


[ExtensionFeatureDataType(typeof(PingPongExtension), "pong-message")]
public class PongMessage {
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
```

The `[ExtensionFeature]` annotation defines a URI for the extension. This can be specified as a relative URI path (in which case it will be made absolute using `WellKnownFeatures.Extensions.ExtensionFeatureBasePath` as the base) or as an absolute URI (in which case it must be a child path of `WellKnownFeatures.Extensions.ExtensionFeatureBasePath`). The URI for the feature always ends with a forwards slash; one will be added if not specified in the URI passed to the `[ExtensionFeature]`. This information is used to create a descriptor for the feature. An example (JSON-encoded) descriptor for the ping-pong extension defined above would look like this:

```json
{
  "uri": "asc:extensions/example/ping-pong/",
  "displayName": "Ping Pong",
  "description": "Responds to every ping message with a pong message"
}
```

When writing an extension feature, methods can be annotated with an [ExtensionFeatureOperationAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureOperationAttribute.cs). When one of the `BindXXX` methods is used to bind the method to an `Invoke`, `Stream`, or `DuplexStream` operation, this attribute is used to generate a descriptor for the operation. An example (JSON-encoded) descriptor for the `Ping` method that is bound to the `Invoke` call above would look like this:

```json
{
    "operationId": "asc:extensions/example/ping-pong/invoke/Ping/",
    "operationType": "Invoke",
    "name": "Ping",
    "description": "Returns a pong message that matches the correlation ID of the specified ping message",
    "inputs": [
        {
            "ordinal": 0,
            "variantType": "ExtensionObject",
            "arrayRank": 0,
            "typeId": "asc:extensions/unit-tests/ping-pong/types/ping-message/",
            "description": "The ping message"
        }
    ],
    "outputs": [
        {
            "ordinal": 0,
            "variantType": "ExtensionObject",
            "arrayRank": 0,
            "typeId": "asc:extensions/unit-tests/ping-pong/types/pong-message/",
            "description": "The resulting pong message"
        }
    ]
}
```


# Providing Adapter Options From Configuration

`AdapterBase<T>` and `AdapterBase` both define constructors that allow the options for the adapter to be supplied via an `IOptions<T>` or `IOptionsMonitor<T>` instance supplied by the configuration system of an ASP.NET Core application, or an application using the .NET Core Generic Host.

For example:

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    }
  },
  "CsvAdapter": {
    "my-csv": {
      "Name": "Sample Data",
      "Description": "CSV adapter with dummy data",
      "IsDataLoopingAllowed": true,
      "CsvFile": "SampleData.csv"
    }
  }
}
```

```csharp
public class Startup {

    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services) {
        // Other configuration removed for brevity.

        // Bind CSV adapter options against the application configuration.
        services.Configure<DataCore.Adapter.Csv.CsvAdapterOptions>(Configuration.GetSection("CsvAdapter:my-csv"));

        services
            .AddDataCoreAdapterAspNetCoreServices()
            .AddHostInfo(HostInfo.Create(
                "My Host",
                "A brief description of the hosting application",
                "0.9.0-alpha", // SemVer v2
                VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
                AdapterProperty.Create("Project URL", "https://github.com/intelligentplant/AppStoreConnect.Adapters")
            ))
            // Create adapter using an IOptions<T> to supply options.
            .AddAdapter<DataCore.Adapter.Csv.CsvAdapter>(sp => ActivatorUtilities.CreateInstance<Csv.CsvAdapter>(
                sp, 
                "my-csv", // Adapter ID 
                sp.GetRequiredService<IOptions<DataCore.Adapter.Csv.CsvAdapterOptions>>()
            ))
            .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
    }

    // Remaining code removed for brevity.

}
```

Note that, when using `IOptionsMonitor<T>`, the adapter will always try and retrieve named options that match the ID of the adapter. That is, if you register an adapter with an ID of `adapter-001`, you must also register named options with the configuration system with a name of `adapter-001`:

```csharp
public class Startup {

    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services) {
        // Other configuration removed for brevity.

        // Bind named CSV adapter options against the application configuration.
        services.Configure<DataCore.Adapter.Csv.CsvAdapterOptions>(
            "my-csv", // Key for this set of options
            Configuration.GetSection("CsvAdapter:my-csv")
        );

        services
            .AddDataCoreAdapterAspNetCoreServices()
            .AddHostInfo(HostInfo.Create(
                "My Host",
                "A brief description of the hosting application",
                "0.9.0-alpha", // SemVer v2
                VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
                AdapterProperty.Create("Project URL", "https://github.com/intelligentplant/AppStoreConnect.Adapters")
            ))
            // Create adapter using an IOptionsMonitor<T> to supply named options.
            .AddAdapter<DataCore.Adapter.Csv.CsvAdapter>(sp => ActivatorUtilities.CreateInstance<Csv.CsvAdapter>(
                sp, 
                "my-csv", // Adapter ID; also used as the named options key   
                sp.GetRequiredService<IOptionsMonitor<DataCore.Adapter.Csv.CsvAdapterOptions>>()
            ))
            .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
    }

    // Remaining code removed for brevity.

}
```
