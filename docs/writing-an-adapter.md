# Writing an Adapter

> The [Creating an Adapter](/docs/tutorials/creating-an-adapter) tutorial provides a walk-through example of how to write an adapter.

To get started, add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter).

An adapter is a component that exposes real-time process data and/or alarm & event data to [Intelligent Plant](https://www.intelligentplant.com) App Store Connect. This data can then be used by apps on the [Industrial App Store](https://appstore.intelligentplant.com) such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).

All adapters implement the [IAdapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs) interface. Each adapter implements a set of *features*, which are exposed via an [IAdapterFeaturesCollection](/src/DataCore.Adapter.Abstractions/IAdapterFeaturesCollection.cs). Individual features are defined as interfaces, and inherit from [IAdapterFeature](/src/DataCore.Adapter.Abstractions/IAdapterFeature.cs). 

Adapter implementations should inherit from the abstract [AdapterBase&lt;TAdapterOptions&gt;](/src/DataCore.Adapter/AdapterBaseT.cs) or [AdapterBase](/src/DataCore.Adapter/AdapterBase.cs) classes. Note that `AdapterBase` is a subclass of `AdapterBase<TAdapterOptions>`.


## Adapter Options

The [AdapterOptions](/src/DataCore.Adapter/AdapterOptions.cs) class is the base class for all adapter configuration options. At its most basic level, it is used to provide the display name and description for an adapter. When writing an adapter, extend the class to provide adapter-specific configuration to your adapter type:

```csharp
public class MyAdapterOptions : AdapterOptions {

    [Required]
    public string Hostname { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 12345;

}


public class MyAdapter : AdapterBase<MyAdapterOptions> {

    public MyAdapter(string id, MyAdapterOptions options) : base(id, options) { }

}
```


# Implementing Features

Adapter implementers can pick and choose which features they want to provide. For example, the `DataCore.Adapter.RealTimeData` namespace defines interfaces for features related to real-time process data (requesting current tag values, performing various types of historical data queries, and so on). An individual adapter can implement features related to process data, alarm and event sources, and alarm and event sinks, as required.

Every feature defines a URI that uniquely identifies the feature. URIs for well-known features are defined [here](/src/DataCore.Adapter.Abstractions/WellKnownFeatures.cs).


## Standard Features

Adapters can implement any number of the following standard feature interfaces:

- Asset Model:
    - [IAssetModelBrowse](/src/DataCore.Adapter.Abstractions/AssetModel/IAssetModelBrowse.cs)
    - [IAssetModelSearch](/src/DataCore.Adapter.Abstractions/AssetModel/IAssetModelSearch.cs)
- Diagnostics:
    - [IConfigurationChanges](/src/DataCore.Adapter.Abstractions/Diagostics/IConfigurationChanges.cs)
    - [IHealthCheck](/src/DataCore.Adapter.Abstractions/Diagostics/IHealthCheck.cs)
- Tags:
    - [ITagInfo](/src/DataCore.Adapter.Abstractions/Tags/ITagInfo.cs)
    - [ITagSearch](/src/DataCore.Adapter.Abstractions/Tags/ITagSearch.cs)
- Events:
    - [IEventMessagePush](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePush.cs)
    - [IEventMessagePushWithTopics](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePushWithTopics.cs)
    - [IReadEventMessagesForTimeRange](/src/DataCore.Adapter.Abstractions/Events/IReadEventMessagesForTimeRange.cs)
    - [IReadEventMessagesUsingCursor](/src/DataCore.Adapter.Abstractions/Events/IReadEventMessagesUsingCursor.cs)
    - [IWriteEventMessages](/src/DataCore.Adapter.Abstractions/Events/IWriteEventMessages.cs)
- Real-Time Data:
    - [IReadPlotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs)
    - [IReadProcessedTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs)
    - [IReadRawTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadRawTagValues.cs)
    - [IReadSnapshotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadSnapshotTagValues.cs)
    - [IReadTagValueAnnotations](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValueAnnotations.cs)
    - [IReadTagValuesAtTimes](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs)
    - [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs)
    - [IWriteHistoricalTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IWriteHistoricalTagValues.cs)
    - [IWriteSnapshotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IWriteSnapshotTagValues.cs)
    - [IWriteTagValueAnnotations](/src/DataCore.Adapter.Abstractions/RealTimeData/IWriteTagValueAnnotations.cs)

Adapters can also implement custom extension features. This is described in more detail below.


## Helper Methods

`AdapterBase<TAdapterOptions>` defines helper methods that should be used in feature implementations:

- The `ValidateInvocation` ensures that the `IAdapterCallContext` and request object passed into a feature method implementation are non-null and pass validation.
- The `CreateCancellationTokenSource` method takes a params array of `CancellationToken` instances and returns a `CancellationTokenSource` that will request cancellation when any of the supplied cancellation tokens request cancellation, or the adapter is stopped.

```csharp
async IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    ValidateInvocation(context, request);

    using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
        await foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token).ConfigureAwait(false)) {
            yield return item;
        }
    }
}

private IAsyncEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
    ...
}
```


## Working with IAsyncEnumerable<T>

Adapter features make extensive use of the `IAsyncEnumerable<T>` type, to allow query results to be streamed back to the caller asynchronously. For .NET Framework and .NET Standard 2.0 targets, the [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) NuGet package is used to define the type.

> In order to produce `IAsyncEnumerable<T>` instances from iterator methods, or to consume `IAsyncEnumerator<T>` instances using `await foreach` loops, your project must use C# 8.0 or higher.

In most cases, it is advisable to declare a feature method using the `async` keyword, and to use `yield return` statements to emit values as they occur. For example:

```csharp
async IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    ValidateInvocation(context, request);

    using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
        await foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token).ConfigureAwait(false)) {
            yield return item;
        }
    }
}

private IAsyncEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
    ...
}
```

If your implementation runs synchronously (e.g. if the return values are held in an in-memory collection), you can use `Task.CompletedTask` to make the implementation asynchronous:

```csharp
async IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    ValidateInvocation(context, request);

    await Task.CompletedTask.ConfigureAwait(false);

    using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
        foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token)) {
            yield return item;
        }
    }
}

private IEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
    ...
}
```


## Delegating Feature Implementations to External Providers

Feature implementations can be delegated to another class instead of being implemented directly by the adapter class. Examples of external feature provider classes include the [SnapshotTagValuePush](/src/DataCore.Adapter/RealTimeData/SnapshotTagValuePush.cs) class, which can be used to add [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) functionality to your adapter.

When using external feature providers, you must register the features manually, by calling the `AddFeature` or `AddFeatures` methods inherited from `AdapterBase<TAdapterOptions>`.

Any features that are added to the adapter from an external provider that implement `IDisposable` or `IAsyncDisposable` will be disposed when the adapter is disposed.


## Disabling Automatic Feature Registration

To disable automatic registration of features implemented directly on the adapter class, you can annotate your class with an [AutomaticFeatureRegistrationAttribute](/src/DataCore.Adapter/AutomaticFeatureRegistrationAttribute.cs):

```csharp
[AutomaticFeatureRegistration(false)]
public class MyAdapter : AdapterBase<MyAdapterOptions> {

}
```

> The `IHealthCheck` feature supplied by `AdapterBase<TAdapterOptions>` will always be registered, even if automatic feature registration is disabled.

If automatic feature registration is disabled, you must manually register any features that are directly implemented by the adapter class:

```csharp
[AutomaticFeatureRegistration(false)]
public class MyAdapter : AdapterBase<MyAdapterOptions>, ITagSearch {

    private class RegisterTagSearchFeature() {
        AddFeature<ITagSearch>(this);
    }

}
```

### Use Cases

In general, is is not desirable to disable automatic feature registration. However, there are some examples of when this behaviour may be required:

- You are writing an adapter for a protocol that supports both the reading and writing of values (e.g. MQTT), but you want to enable and disable writing to the remote server at runtime based on the supplied adapter options.
- You are writing a proxy for an adapter hosted in an external system, and you want to add features to your adapter at runtime that match the features implemented by the remote adapter.


## Health Checks (IHealthCheck Feature)

`AdapterBase<TAdapterOptions>` adds out-of-the-box support for the [IHealthCheck](/src/DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs) feature. To customise the health checks that are performed, you can override the `CheckHealthAsync` method.

Whenever the health status of your adapter changes (e.g. you become disconnected from an external service that the adapter relies on), you should call the `OnHealthStatusChanged` method from your implementation. This will recompute the overall health status of the adapter and push the update to any subscribers to the `IHealthCheck` feature.


## Event Message Subscriptions (IEventMessagePush / IEventMessagePushWithTopics Features)

To add the [IEventMessagePush](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePush.cs) and/or [IEventMessagePushWithTopics](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePushWithTopics.cs) features to your adapter, you can add or extend the [EventMessagePush](/src/DataCore.Adapter/Events/EventMessagePush.cs) and [EventMessagePushWithTopics](/src/DataCore.Adapter/Events/EventMessagePushWithTopics.cs) classes respectively. To push values to subscribers, call the `ValueReceived` method on the feature.

If your source supports its own subscription mechanism, you can extend the `EventMessagePush` and/or `EventMessagePushWithTopics` classes and override the appropriate extension points. For example, in an OPC UA adapter, you could extend `EventMessagePushWithTopics` to add a new monitored item to an OPC UA subscription when a subscription to a topic was created on your adapter.


## Snapshot Tag Value Subscriptions (ISnapshotTagValuePush Feature)

To add the [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) feature to your adapter, you can use the [SnapshotTagValuePush](/src/DataCore.Adapter/RealTimeData/SnapshotTagValuePush.cs) or [PollingSnapshotTagValuePush](/src/DataCore.Adapter/RealTimeData/PollingSnapshotTagValuePush.cs) classes. The latter can be used when the underlying source does not support a subscription mechanism of its own, and allows subscribers to your adapter to receive real-time value changes at an update rate of your choosing, by polling the underlying source for values on a periodic basis. To push values to subscribers, call the `ValueReceived` method on the feature.

If your source supports its own subscription mechanism, you can extend the `SnapshotTagValuePush` class and override the appropriate extension points. For example, if you were writing an MQTT adapter that treats individual MQTT channels as tags, you could extend `SnapshotTagValuePush` so that it subscribes to an MQTT channel when a subscriber subscribes to a given tag name.


## Historical Tag Value Queries 

If your underlying source does not support aggregated, values-at-times, or plot/best-fit tag value queries (implemented via the [IReadProcessedTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs), [IReadTagValuesAtTimes](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs), and [IReadPlotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) respectively), you can use the [ReadHistoricalTagValues](/src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) class to provide these capabilities, as long as you can provide it with the ability to resolve tag names, and to request raw tag values.

If your source implements some of these capabilities but not others, you can use the classes in the `DataCore.Adapter.RealTimeData.Utilities` namespace to assist with the implementation of the missing functionality if desired.

> Note that using `ReadHistoricalTagValues` or the associated utility classes will almost certainly perform worse than a native implementation; native implementations are always encouraged where available.


## Extension Features

> The [Writing an Extension Feature](/docs/tutorials/writing-an-extension-feature) tutorial provides a walk-through example of how to write an extension feature for an adapter.

In addition to standard features, implementers can define their own extension features.

Extension features must inherit from [IAdapterExtensionFeature](/src/DataCore.Adapter.Abstractions/Extensions/IAdapterExtensionFeature.cs), and must be annotated with an [ExtensionFeatureAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureAttribute.cs), which identifies the URI for the extension, as well as additional properties such as the display name and description.

The `IAdapterExtensionFeature` interface defines methods for retrieving a descriptor for the extension, and a list of available operations. Extension operations are called via the `Invoke`, `Stream`, or `DuplexStream` methods defined on `IAdapterExtensionFeature`. 

The [AdapterExtensionFeature](/src/DataCore.Adapter/Extensions/AdapterExtensionFeature.cs) class is a base class for simplifying the implementation of extension features, which provides a number of `BindInvoke`, `BindStream`, and `BindDuplexStream` methods to automatically generate operation descriptors for the extension feature, and to automatically invoke the bound method when a call is made to the extension's `Invoke`, `Stream`, or `DuplexStream` methods.

For example, the full implementation of a "ping pong" extension, that responds to `PingMessage` objects it receives with an equivalent `PongMessage` might look like this:

```csharp
[ExtensionFeature(
    // Relative feature URI; will be made absolute relative to WellKnownFeatures.Extensions.ExtensionFeatureBasePath
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
    public PongMessage PingInvoke(PingMessage ping) {
        if (ping == null) {
            throw new ArgumentNullException(nameof(ping));
        }

        return new PongMessage() {
            CorrelationId = ping.CorrelationId
        };
    }


    [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingStreamDescriptor))]
    public async IAsyncEnumerable<PongMessage> PingStream(
        PingMessage ping,
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        if (ping == null) {
            throw new ArgumentNullException(nameof(ping));
        }

        while (!cancellationToken.IsCancellationRequested) {
            try {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                yield return PingInvoke(ping);
            }
            catch (OperationCanceledException) { }
        }
    }


    [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingDuplexStreamDescriptor))]
    public async IAsyncEnumerable<PongMessage> PingDuplexStream(
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
            yield return PingInvoke(ping);
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


[ExtensionFeatureDataType(
    // The extension feature that this data type belongs to.
    typeof(PingPongExtension), 
    // Type identifier. Will be made absolute relative to the /types path under the feature URI.
    "ping-message"
)]
public class PingMessage {
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}


[ExtensionFeatureDataType(
    // The extension feature that this data type belongs to.
    typeof(PingPongExtension), 
    // Type identifier. Will be made absolute relative to the /types path under the feature URI.
    "pong-message"
)]
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

When writing an extension feature, methods can be annotated with an [ExtensionFeatureOperationAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureOperationAttribute.cs). When one of the `BindXXX` methods is used to bind the method to an `Invoke`, `Stream`, or `DuplexStream` operation, this attribute is used to generate a descriptor for the operation. 

For example, the `PingInvoke` method above is annotated with an `[ExtensionFeatureOperation]` attribute that uses the static `GetPingInvokeDescriptor` method to retrieve metadata about the operation. An example (JSON-encoded) descriptor for the operation generated by the `AdapterExtensionFeature` base class would look like this:

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
            "typeId": "asc:extensions/example/ping-pong/types/ping-message/",
            "description": "The ping message"
        }
    ],
    "outputs": [
        {
            "ordinal": 0,
            "variantType": "ExtensionObject",
            "arrayRank": 0,
            "typeId": "asc:extensions/example/ping-pong/types/pong-message/",
            "description": "The resulting pong message"
        }
    ]
}
```


# Telemetry

Telemetry is provided using the [ActivitySource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource) class (via the [System.Diagnostics.DiagnosticSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource) NuGet package). The [Web API](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Mvc), [gRPC](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Grpc), and [SignalR](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR) hosting packages automatically create activities when invoking operations on your adapter. You can use `ActivitySource` property in the static [Telemetry](/src/DataCore.Adapter/Diagnostics/Telemetry.cs) class to provide adapter-specific telemetry inside your feature implementations (for example, while executing a database query):

```csharp
private async IAsyncEnumerable<EventMessage> ReadEventMessages(
    SqlCommand command, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    using (var conn = await OpenConnection(cancellationToken).ConfigureAwait(false)) {
        command.Connection = conn;

        using (var executeReaderActivity = Telemetry.ActivitySource.StartActivity("my_adapter/db_execute_reader"))
        using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false)) {
            using (Telemetry.ActivitySource.StartActivity("my_adapter/db_reader_read", ActivityKind.Internal, executeReaderActivity?.Id!)) {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                    var item = await ReadEventMessage(reader, cancellationToken).ConfigureAwait(false);

                    yield return item;
                }
            }
        }
    }
}
```


# Providing Adapter Options From Configuration

`AdapterBase<TAdapterOptions>` and `AdapterBase` both define constructors that allow the options for the adapter to be supplied via an `IOptions<T>` or `IOptionsMonitor<T>` instance supplied by the configuration system of an ASP.NET Core application, or an application using the .NET Core Generic Host. If you implement an appropriate constructor in your adapter implementation, you can receive pass options into your adapter at startup using this mechanism. For example:

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

Passing options to your adapter using an `IOptionsMonitor<T>` also allows you to reconfigure your adapter at runtime when the configuration options change in the ASP.NET Core or generic host application. You can react to configuration changes by overriding the `OnOptionsChange` method in your adapter implementation.


# Structuring Adapter Code

When writing an adapter that implements multiple features, it can be useful to segregate the different feature implementations for readability purposes. Partial classes provide an excellent way of segregating the implementation code, while ensuring that each feature can access all of the helper methods inherited from `AdapterBase<TAdapterOptions>`.

```csharp
// MyAdapter.cs

public partial class MyAdapter : AdapterBase<MyAdapterOptions> {
    ...
}
```

```csharp
// MyAdapter.ReadSnapshotTagValues.cs

partial class MyAdapter : IReadSnapshotTagValues {

    async IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
        IAdapterCallContext context, 
        ReadSnapshotTagValuesRequest request, 
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        ...
    }

}
```


# Testing

A [helper package](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers) is available to assist with basic testing of adapters using MSTest. To write tests for your adapter, extend the [AdapterTestsBase&lt;TAdapter&gt;](/src/DataCore.Adapter.Tests.Helpers/AdapterTestsBase.cs) base class, annotate your new class with a `[TestClass]` attribute, implement the abstract `CreateServiceScope` and `CreateAdapter` methods, and then override the various `CreateXXXRequest` methods to supply settings for the features that your adapter implements:

```csharp
// AssemblyInitializer.cs

[TestClass]
public class AssemblyInitializer {

    public static IServiceProvider? ServiceProvider { get; private set; }

    [AssemblyInitialize]
    public static void Init(TestContext testContext) {
        // This method is called by MSTest when your test assembly is loaded. If you will not 
        // be using a service provider when creating adapter instances for use in tests, this 
        // class is not required.

        var services = new ServiceCollection();

        // TODO: add services such as logging here.

        ServiceProvider = services.BuildServiceProvider();
    }

}
```

```csharp
// MyAdapterTests.cs

[TestClass]
public class MyAdapterTests : AdapterTestsBase<MyAdapter> {

    protected override IServiceScope? CreateServiceScope(TestContext context) {
        // Create and return a service scope for a test. You can return null if you will not be 
        // using a service provider when creating your adapter instances in CreateAdapter below.
        return AssemblyInitializer.ServiceProvider?.CreateScope();
    }

    protected override MyAdapter CreateAdapter(TestContext context, IServiceProvider? serviceProvider) {
        // Create and return an instance of your adapter here. The 'serviceProvider' parameter will
        // be null if CreateServiceScope above returns null.
    }

    protected override ReadSnapshotTagValuesRequest CreateReadSnapshotTagValuesRequest(TestContext context) {
        // If your adapter implements IReadSnapshotTagValues, you can supply a request object to 
        // use when testing that feature.
        return new ReadSnapshotTagValuesRequest() {
            Tags = new[] { "Example_Tag_1", "Example_Tag_2" }
        };
    }

    // TODO: write your own adapter-specific tests

}
```
