# Writing an Adapter

> We __strongly__ recommend that you create self-hosted adapters using the provided template for Visual Studio and `dotnet new`. See [here](../src/DataCore.Adapter.Templates) for more information.

An adapter is a component that exposes real-time process data and/or alarm & event data to [Intelligent Plant](https://www.intelligentplant.com) App Store Connect. This data can then be used by apps on the [Industrial App Store](https://appstore.intelligentplant.com) such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).

You can find a tutorial for writing a simple MQTT adapter [here](./tutorials/mqtt-adapter).

All adapters implement the [IAdapter](../src/DataCore.Adapter.Abstractions/IAdapter.cs) interface. Each adapter implements a set of *features*, which are exposed via an [IAdapterFeaturesCollection](../src/DataCore.Adapter.Abstractions/IAdapterFeaturesCollection.cs). Individual features are defined as interfaces, and inherit from [IAdapterFeature](../src/DataCore.Adapter.Abstractions/IAdapterFeature.cs).

> Note that adapters do not have to directly implement the feature interfaces themselves. Instead, the adapter can delegate the feature implementation to a helper class. This is described in more detail [below](#delegating-feature-implementations-to-external-providers).

Adapter implementations should inherit from the abstract [AdapterCore](../src/DataCore.Adapter.Abstractions/AdapterCore.cs), [AdapterBase&lt;TAdapterOptions&gt;](../src/DataCore.Adapter/AdapterBaseT.cs) or [AdapterBase](../src/DataCore.Adapter/AdapterBase.cs) classes. Inheriting from `AdapterBase<TAdapterOptions>` or `AdapterBase` is recommended.


## Adapter Options

The [AdapterOptions](../src/DataCore.Adapter/AdapterOptions.cs) class is the base class for all adapter configuration options. At its most basic level, it is used to provide the display name and description for an adapter. When writing an adapter, extend the class to provide adapter-specific configuration to your adapter type:

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

Every feature defines a URI that uniquely identifies the feature. URIs for well-known features are defined [here](../src/DataCore.Adapter.Abstractions/WellKnownFeatures.cs).


## Available Features

Adapters can implement any number of the following standard feature interfaces:

| Category | Name | Description |
| -------- | ---- | ----------- |
| Asset Model | [IAssetModelBrowse](../src/DataCore.Adapter.Abstractions/AssetModel/IAssetModelBrowse.cs) | Asset model browsing |
| Asset Model | [IAssetModelSearch](../src/DataCore.Adapter.Abstractions/AssetModel/IAssetModelSearch.cs) | Asset model search |
| Custom Functions | [ICustomFunctions](../src/DataCore.Adapter.Abstractions/Extensions/ICustomFunctions.cs) | Vendor- or adapter-specific custom RPC functions |
| Diagnostics | [IConfigurationChanges](../src/DataCore.Adapter.Abstractions/Diagnostics/IConfigurationChanges.cs) | Notifications about changes to an adapter's available tags, assets, etc. |
| Diagnostics | [IHealthCheck](../src/DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs) | Reports the health status of the adapter and its external dependencies. |
| Events | [IEventMessagePush](../src/DataCore.Adapter.Abstractions/Events/IEventMessagePush.cs) | Push subscriptions that notify callers about events in real-time. |
| Events | [IEventMessagePushWithTopics](../src/DataCore.Adapter.Abstractions/Events/IEventMessagePushWithTopics.cs) | Push subscriptions that notify callers about events in real-time via topics. |
| Events | [IReadEventMessagesForTimeRange](../src/DataCore.Adapter.Abstractions/Events/IReadEventMessagesForTimeRange.cs) | Retrieval of historical event messages within a given time range. |
| Events | [IReadEventMessagesUsingCursor](../src/DataCore.Adapter.Abstractions/Events/IReadEventMessagesUsingCursor.cs) | Retrieval of historical event messages starting from a given cursor position. |
| Events | [IWriteEventMessages](../src/DataCore.Adapter.Abstractions/Events/IWriteEventMessages.cs) | Ingestion of event messages from an external source. |
| Real-Time Data | [IReadPlotTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) | Retrieval of a best-fit curve of raw historical tag values for visualization in a chart. |
| Real-Time Data | [IReadProcessedTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs) | Retrieval of aggregated tag values (such as the average value of a tag over an hourly interval), and for discovering the aggregations that the adapter supports. |
| Real-Time Data | [IReadRawTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadRawTagValues.cs) | Polling of raw, unprocessed historical tag values. |
| Real-Time Data | [IReadSnapshotTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadSnapshotTagValues.cs) | Polling of the current tag values. |
| Real-Time Data | [IReadTagValueAnnotations](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValueAnnotations.cs) | Retrieval of annotations associated with tag values (such as when a value exceeded its operating limits). |
| Real-Time Data | [IReadTagValuesAtTimes](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs) | Retrieval of tag values at specific points in history. |
| Real-Time Data | [ISnapshotTagValuePush](../src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) | Push subscriptions that notify callers about changes in the current values for subscribed tags. |
| Real-Time Data | [IWriteHistoricalTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IWriteHistoricalTagValues.cs) | Ingestion of tag values directly into a historical archive. |
| Real-Time Data | [IWriteSnapshotTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IWriteSnapshotTagValues.cs) | Ingestion of tag values into a snapshot pipeline where data filters can be used to determine when values should be written to a historical archive. |
| Real-Time Data | [IWriteTagValueAnnotations](../src/DataCore.Adapter.Abstractions/RealTimeData/IWriteTagValueAnnotations.cs) | Management of annotations associated with tag values. |
| Tags | [ITagConfiguration](../src/DataCore.Adapter.Abstractions/Tags/ITagConfiguration.cs) | Management of tag definitions using adapter-specific schemas. |
| Tags | [ITagInfo](../src/DataCore.Adapter.Abstractions/Tags/ITagInfo.cs) | Retrieval of information about tags by ID or name. |
| Tags | [ITagSearch](../src/DataCore.Adapter.Abstractions/Tags/ITagSearch.cs) | Discovery of tags via search operations. |

The [ICustomFunctions](../src/DataCore.Adapter.Abstractions/Extensions/ICustomFunctions.cs) feature allows an adapter to define bespoke custom functions that can be invoked via standard API calls. This is described in more detail below.



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
    await foreach (var item in GetSnapshotValues(request.Tags, cancellationToken).ConfigureAwait(false)) {
        yield return item;
    }
}

private IAsyncEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
    ...
}
```

If your implementation runs synchronously (e.g. if the return values are held in an in-memory collection), you can use `Task.Yield` to make the implementation asynchronous:

```csharp
async IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    await Task.Yield();

    foreach (var item in GetSnapshotValues(request.Tags, cancellationToken)) {
        yield return item;
    }
}

private IEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
    ...
}
```


## Feature Wrappers

All features that are registered with an adapter derived from `AdapterCore` are wrapped inside a special wrapper class that performs the following functions:

- Validates the `IAdapterCallContext` parameter and any request or client streaming parameters passed to each feature method.
- Generates telemetry ([Activity](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity), metrics, and [EventSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource)) for each feature invocation. In the case of server and client streaming methods, metrics and `EventSource` logging is generated for each item emitted or ingested by the method.

If you need to access the underlying feature implementation (for example, if it is an external implementation as described below and you need access to another method on the external implementation) you can use the `Unwrap` extension method on the wrapper to get the original feature implementation:

```csharp
internal class MyTagSearchFeature : ITagSearch {
  // ITagSearch code removed for brevity
  public void DeleteAllTags() {
    // Implementation removed for brevity
  }
}

internal static void DeleteAllTagsForAdapter(IAdapter adapter) {
    if (!(adapter.GetFeature<ITagSearch>().Unwrap() is MyTagSearchFeature feature) {
        return;
    }

    feature.DeleteAllTags();
}
```


## Delegating Feature Implementations to External Providers

Feature implementations can be delegated to another class instead of being implemented directly by the adapter class. Examples of external feature provider classes include the [SnapshotTagValuePush](../src/DataCore.Adapter/RealTimeData/SnapshotTagValuePush.cs) class, which can be used to add [ISnapshotTagValuePush](../src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) functionality to your adapter. Examples of additional external providers can be found in the sections below.

When using external feature providers, you must register the features manually, by calling the `AddFeature` or `AddFeatures` methods inherited from `AdapterCore`:

```csharp
var snapshotPush = new PollingSnapshotTagValuePush(this, new PollingSnapshotTagValuePushOptions() { 
    AdapterId = Descriptor.Id,
    PollingInterval = TimeSpan.FromSeconds(5),
    TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
}, BackgroundTaskService, LoggerFactory.CreateLogger<PollingSnapshotTagValuePush>());

AddFeatures(snapshotPush);
```

> Any features that are added to the adapter from an external provider that implement `IDisposable` or `IAsyncDisposable` will be disposed when the adapter is disposed.


## Health Checks (IHealthCheck Feature)

`AdapterBase<TAdapterOptions>` adds out-of-the-box support for the [IHealthCheck](../src/DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs) feature. To customise the health checks that are performed, you can override the `CheckHealthAsync` method.

Whenever the health status of your adapter changes (e.g. you become disconnected from an external service that the adapter relies on), you should call the `OnHealthStatusChanged` method from your implementation. This will recompute the overall health status of the adapter and push the update to any subscribers to the `IHealthCheck` feature.


## Tag Management (ITagInfo, ITagSearch Features)

*This topic is described in more detail [here](./features/tag-search.md).*

If your adapter will manage its own tag definitions instead of retrieving them from e.g. an external database, you can use the [TagManager](../src/DataCore.Adapter/Tags/TagManager.cs) class to implement the [ITagInfo](../src/DataCore.Adapter.Abstractions/Tags/ITagInfo.cs) and [ITagSearch](../src/DataCore.Adapter.Abstractions/Tags/ITagInfo.cs) features on your adapter's behalf.


## Asset Model Management (IAssetModelBrowse, IAssetModelSearch Features)

If your adapter must manage its own asset model, you can delegate this functionality to the [AssetModelManager](../src/DataCore.Adapter/AssetModel/AssetModelManager.cs) class. `AssetModelManager` implements the [IAssetModelBrowse](../src/DataCore.Adapter.Abstractions/AssetModel/IAssetModelBrowse.cs) and [IAssetModelSearch](../src/DataCore.Adapter.Abstractions/AssetModel/IAssetModelSearch.cs) features on your adapter's behalf.


## Configuration Changes (IConfigurationChanges Feature)

The [ConfigurationChanges](../src/DataCore.Adapter/Diagnostics/ConfigurationChanges.cs) class can be used to implement the [IConfigurationChanges](../src/DataCore.Adapter.Abstractions/Diagnostics/IConfigurationChanges.cs) feature on your adapter's behalf.

[TagManager](../src/DataCore.Adapter/Tags/TagManager.cs) and [AssetModelManager](../src/DataCore.Adapter/AssetModel/AssetModelManager.cs) can integrate with `ConfigurationChanges` to send notifications when tags or asset model nodes are created, updated or deleted.


## Event Message Subscriptions (IEventMessagePush / IEventMessagePushWithTopics Features)

To add the [IEventMessagePush](../src/DataCore.Adapter.Abstractions/Events/IEventMessagePush.cs) and/or [IEventMessagePushWithTopics](../src/DataCore.Adapter.Abstractions/Events/IEventMessagePushWithTopics.cs) features to your adapter, you can add or extend the [EventMessagePush](../src/DataCore.Adapter/Events/EventMessagePush.cs) and [EventMessagePushWithTopics](../src/DataCore.Adapter/Events/EventMessagePushWithTopics.cs) classes respectively. To push values to subscribers, call the `ValueReceived` method on the feature.

If your source supports its own subscription mechanism, you can extend the `EventMessagePush` and/or `EventMessagePushWithTopics` classes and override the appropriate extension points. For example, in an OPC UA adapter, you could extend `EventMessagePushWithTopics` to add a new monitored item to an OPC UA subscription when a subscription to a topic was created on your adapter.


## Snapshot Tag Value Subscriptions (ISnapshotTagValuePush Feature)

*This topic is described in more detail [here](./features/tag-snapshot-polling-and-subscriptions.md).*

To add the [ISnapshotTagValuePush](../src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) feature to your adapter, you can use the [SnapshotTagValuePush](../src/DataCore.Adapter/RealTimeData/SnapshotTagValuePush.cs) or [PollingSnapshotTagValuePush](../src/DataCore.Adapter/RealTimeData/PollingSnapshotTagValuePush.cs) classes. The latter can be used when the underlying source does not support a subscription mechanism of its own, and allows subscribers to your adapter to receive real-time value changes at an update rate of your choosing, by polling the underlying source for values on a periodic basis. To push values to subscribers, call the `ValueReceived` method on the feature.

If your source supports its own subscription mechanism, you can extend the `SnapshotTagValuePush` class and override the appropriate extension points. For example, if you were writing an MQTT adapter that treats individual MQTT channels as tags, you could extend `SnapshotTagValuePush` so that it subscribes to an MQTT channel when a subscriber subscribes to a given tag name.

Note that you can also use the [SnapshotTagValueManager](../src/DataCore.Adapter/RealTimeData/SnapshotTagValueManager.cs) class to implement both [ISnapshotTagValuePush](../src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) _and_ [IReadSnapshotTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadSnapshotTagValues.cs) on your adapter's behalf. This is useful when your source does not support direct polling and you need to cache snapshot values received via push locally in the adapter.


## Historical Tag Value Queries 

*This topic is described in more detail [here](./features/tag-history-polling.md).*

If your underlying source does not natively support aggregated, values-at-times, or plot/best-fit tag value queries (implemented via the [IReadProcessedTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs), [IReadTagValuesAtTimes](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs), and [IReadPlotTagValues](../src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) features respectively), you can use the [ReadHistoricalTagValues](../src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) class to provide these capabilities, as long as you can provide it with the ability to resolve tag names, and to request raw tag values.

If your source implements some of these capabilities but not others, you can use the classes in the `DataCore.Adapter.RealTimeData.Utilities` namespace to assist with the implementation of the missing functionality if desired.

> Note that using `ReadHistoricalTagValues` or the associated utility classes will almost certainly perform worse than a native implementation; native implementations are always encouraged where available.


## Custom Functions (ICustomFunctions Feature)

An adapter can expose non-standard or vendor-specific functionality via custom functions. Custom functions can be discovered and invoked if an adapter implements the [ICustomFunctions](../src/DataCore.Adapter.Abstractions/Extensions/ICustomFunctions.cs) feature.

The [CustomFunctions](../src/DataCore.Adapter/Extensions/CustomFunctions.cs) class provides an implementation of this feature that can be used to manage custom function registrations. The simpliest way to use the `CustomFunctions` class is to derive your adapter from the `AdapterBase<TAdapterOptions>` base class. The base class exposes a `CustomFunctions` property that can be used to access the custom functions manager for the adapter.

You can then use the `RegisterFunctionAsync` method on the `CustomFunctions` property to register your functions:

```csharp
private async Task AddFunctionsAsync() {
    await CustomFunctions.RegisterFunctionAsync<GreeterRequest, GreeterResponse>(
        "Greet",
        "Replies to requests with a greeting message.",
        (context, request, ct) => Task.FromResult(new GreeterResponse() { 
            Message = $"Hello, {request.Name}!" 
        });
    );
}

public class GreeterRequest {

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

}

public class GreeterResponse {

    public string Message { get; set; } = default!;

}
```

Each registered function has a unique URI identifier. The URI does not have to support dereferencing (i.e. it does not have to be a URL that can be accessed via an HTTP request). 

If a relative URI is specified when registering a function, it will be made absolute using the base URI for the custom functions manager (i.e. the adapter's type descriptor ID). In the example above, the URI will be derived from the base URI and the name of the function. 

Each custom function definition also contains JSON schemas describing valid request and response messages. In the example above, the schemas are automatically generated from the `GreeterRequest` and `GreeterResponse` types. 

Assuming that the type ID of the adapter is `https://my-company.com/app-store-connect/adapters/my-adapter`, the JSON-encoded function description for the `Greet` function would be as follows:

```json
{
    "id": "https://my-company.com/app-store-connect/adapters/my-adapter/custom-functions/greet",
    "name": "Greet",
    "description": "Replies to requests with a greeting message.",
    "requestSchema": {
        "type": "object",
        "properties": {
            "name": {
                "type": "string",
                "maxLength": 100
            }
        },
        "required": [
            "name"
        ]
    },
    "responseSchema": {
        "type": "object",
        "properties": {
            "message": {
                "type": "string"
            }
        }
    }
}
```

The request schema is automatically applied to incoming invocation requests received via the [REST API](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Mvc), [gRPC](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Grpc), and [SignalR](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR) hosting packages.

Example invocations via REST interface:

```
POST /api/app-store-connect/v2.0/custom-functions/my-adapter
Content-Type: application/json

{
    "id": "https://my-company.com/app-store-connect/adapters/my-adapter/custom-functions/greet",
    "body": {
        "name": "John Smith"
    }
}

---

200/OK
Content-Type: application/json

{
    "body": {
        "message": "Hello, John Smith!"
    }
}
```

```
POST /api/app-store-connect/v2.0/custom-functions/my-adapter
Content-Type: application/json

{
    "id": "https://my-company.com/app-store-connect/adapters/my-adapter/custom-functions/greet",
    "body": {
        "name": null
    }
}

---

400/Bad Request
Content-Type: application/json

{
    "valid": false,
    "keywordLocation": "#/properties/name/type",
    "instanceLocation": "#/name",
    "error": "Value is \"null\" but should be \"string\""
}
```


## Disabling Automatic Feature Registration

To disable automatic registration of features implemented directly on the adapter class, you can annotate your class with an [AutomaticFeatureRegistrationAttribute](../src/DataCore.Adapter/AutomaticFeatureRegistrationAttribute.cs):

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


# Persisting State

The [IKeyValueStore](../src/DataCore.Adapter.Abstractions/Services/IKeyValueStore.cs) service can be injected into an adapter constructor to provide a service for storing arbitrary key-value pairs that can be persisted and restored when an adapter or host application is restarted. 
> The default [in-memory implementation](../src/DataCore.Adapter.Abstractions/Services/InMemoryKeyValueStore.cs) does not persist state between restarts of the host application. If you require such durability, you can use one of the implementations listed below or write your own implementation.

The following `IKeyValueStore` implementations support persistence:

- [File System](../src/DataCore.Adapter.KeyValueStore.FileSystem)
- [SQLite](../src/DataCore.Adapter.KeyValueStore.Sqlite)
- [Microsoft FASTER](../src/DataCore.Adapter.KeyValueStore.FASTER)

The implementations above that support persistence serialize values to/from JSON using [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview) and can be configured to automatically compress the serialized bytes using GZip compression.

# Telemetry

## Traces

Tracing is provided using the [ActivitySource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource) class (via the [System.Diagnostics.DiagnosticSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource) NuGet package). 

Activities are automatically created when invoking features that have been registered with an adapter derived from `AdapterCore` (including both `AdapterBase<TAdapterOptions>` and `AdapterBase`).

You can use `ActivitySource` property in the static [Telemetry](../src/DataCore.Adapter.Abstractions/Diagnostics/Telemetry.cs) class to provide adapter-specific trace activities inside your feature implementations (for example, while executing a database query):

```csharp
private async IAsyncEnumerable<EventMessage> ReadEventMessagesAsync(
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


## Metrics

You can use `Meter` property in the static [Telemetry](../src/DataCore.Adapter.Abstractions/Diagnostics/Telemetry.cs) class to add custom metric instrumentation to your adapter.

Instrumentation is automatically generated for the following metrics on all features registered with an adapter derived from `AdapterCore`:

- Operations started
- Operations completed
- Operations faulted
- Operation duration
- Server stream items emitted
- Client stream items consumed


# Providing Adapter Options From Configuration

`AdapterBase<TAdapterOptions>` defines constructors that allow the options for the adapter to be supplied via an `IOptions<T>` or `IOptionsMonitor<T>` instance supplied by your application's dependency injection system. For example:

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
      "Protocols": "Http1AndHttp2AndHttp3"
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
[assembly: DataCore.Adapter.VendorInfo("My Company", "https://my-company.com")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo(
       name: "My Host",
       description: "A brief description of the hosting application"
     )
    // Bind adapter options against the application configuration.
    .AddServices(svc => svc.Configure<DataCore.Adapter.Csv.CsvAdapterOptions>(
        "my-csv",
        builder.Configuration.GetSection("CsvAdapter:my-csv")
     ))
    // Register the adapter.
    .AddAdapter(sp => ActivatorUtilities.CreateInstance<DataCore.Adapter.Csv.CsvAdapter>(
      sp, 
      "my-csv",
      sp.GetRequiredService<IOptionsMonitor<DataCore.Adapter.Csv.CsvAdapterOptions>>()
    ));

// Remaining code removed for brevity.
```

Passing options to your adapter using an `IOptionsMonitor<T>` also allows you to reconfigure your adapter at runtime when the configuration options change in the host application. You can react to configuration changes by overriding the `OnOptionsChange` method in your adapter implementation.


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

A [helper package](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers) is available to assist with basic testing of adapters using MSTest. To write tests for your adapter, extend the [AdapterTestsBase&lt;TAdapter&gt;](../src/DataCore.Adapter.Tests.Helpers/AdapterTestsBase.cs) base class, annotate your new class with a `[TestClass]` attribute, implement the abstract `CreateServiceScope` and `CreateAdapter` methods, and then override the various `CreateXXXRequest` methods to supply settings for the features that your adapter implements:

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
