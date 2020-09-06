# DataCore.Adapter

Contains base classes and utility classes for implementing an [IAdapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs).

Extend from [AdapterBase](./AdapterBase.cs) for easy implementation.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter).


# Creating an Adapter

Extend [AdapterBase<T>](./AdapterBaseT.cs) or [AdapterBase](./AdapterBase.cs) to inherit a base adapter implementation. In both cases, you must implement the `StartAsync` and `StopAsync` methods at a bare minimum.


# Implementing Features

Standard adapter features are defined as interfaces in the [DataCore.Adapter.Abstractions](/src/DataCore.Adapter.Abstractions) project. Any features that are implemented directly on your adapter class will be added to the adapter's feature collection automatically. If you delegate a feature implementation to another class (for example, by using the [SnapshotTagValuePush](./RealTimeData/SnapshotTagValuePush.cs) class to add [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) functionality to your adapter), you must register this feature yourself, by calling the `AddFeature` or `AddFeatures` method inherited from `AdapterBase`.


## IHealthCheck

Both `AdapterBase` and `AdapterBase<T>` add out-of-the-box support for the [IHealthCheck](/src/DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs) feature. To customise the health checks that are performed, you can override the `CheckHealthAsync` method.

Whenever the health status of your adapter changes (e.g. you become disconnected from an external service that the adapter relies on), you should call the `OnHealthStatusChanged` method from your implementation. This will recompute the overall health status of the adapter and push the update to any subscribers to the `IHealthCheck` feature.


## IEventMessagePush

To add the [IEventMessagePush](/src/DataCore.Adapter.Abstractions/Events/IEventMessagePush.cs) feature to your adapter, you can add or extend the [EventMessagePush](/src/DataCore.Adapter.Abstractions/Events/EventMessagePush.cs) class. To push values to subscribers, call the `ValueReceived` method on your `EventMessagePush` object.

If your source supports its own subscription mechanism, you can extend the `EventMessagePush` class to interface with it in the appropriate extension points. In particular, you can override the `CreateSubscription` method if you need to customise the behaviour of the subscription class itself. An example of when you might want to use this option would be if the source you are connecting to requires you to create a separate, authenticated stream for each subscriber, and you prefer to encapsulate that logic inside the subscription class.


## ISnapshotTagValuePush

To add the [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) feature to your adapter, you can use the [SnapshotTagValuePush](./RealTimeData/SnapshotTagValuePush.cs) or [PollingSnapshotTagValuePush](./RealTimeData/PollingSnapshotTagValuePush.cs) classes. The latter can be used when the underlying source does not support a subscription mechanism of its own, and allows subscribers to your adapter to receive real-time value changes at an update rate of your choosing, by polling the underlying source for values on a periodic basis.

If your source supports its own subscription mechanism, you can extend the `SnapshotTagValuePush` class to interface with it in the appropriate extension points. In particular, you can override the `CreateSubscription` method if you need to customise the behaviour of the subscription class itself. An example of when you might want to use this option would be if the source you are connecting to requires you to create a separate, authenticated stream for each subscriber, and you prefer to encapsulate that logic inside the subscription class.

To push values to subscribers, call the `ValueReceived` method on your `SnapshotTagValuePush` instance. When working directly with subscription objects, you can call the `ValueReceived` method on the subscription.


## Historical Data Queries 

If your underlying source does not support aggregated, values-at-times, or plot data queries (implemented via the [IReadProcessedTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs), [IReadTagValuesAtTimes](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs), and [IReadPlotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) respectively), you can use the [ReadHistoricalTagValues](./RealTimeData/ReadHistoricalTagValues.cs) class to provide these capabilities, as long as you can provide it with the ability to resolve tag names, and to request raw tag values.

If your source implements some of these capabilities but not others, you can use the classes in the `DataCore.Adapter.RealTimeData.Utilities` namespace to assist with the implementation of the missing functionality if desired.

Note that using `ReadHistoricalTagValues` or the associated utility classes will almost certainly perform worse than a native implementation; native implementations are always encouraged where available.


# Extension Features

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

    public PingPongExtension(AdapterBase adapter) : this(adapter.BackgroundTaskService) { }

    public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
        BindInvoke<PingMessage, PongMessage>(Ping);
        BindStream<PingMessage, PongMessage>(Ping);
        BindDuplexStream<PingMessage, PongMessage>(Ping);
    }

    // Invoke
    [ExtensionFeatureOperation(
        Name = "Ping",
        Description = "Performs a ping operation on the adapter.",
        InputParameterDescription = "The ping message.",
        OutputParameterDescription = "The pong message."
    )]
    public PongMessage Ping(PingMessage message) {
        if (message == null) {
            throw new ArgumentNullException(nameof(message));
        }

        return new PongMessage() {
            CorrelationId = message.CorrelationId
        };
    }

    // Stream
    [ExtensionFeatureOperation(
        Name = "Ping",
        Description = "Performs a streaming ping operation on the adapter.",
        InputParameterDescription = "The ping message.",
        OutputParameterDescription = "The pong message stream."
    )]
    public Task<ChannelReader<PongMessage>> Ping(
        PingMessage message, 
        CancellationToken cancellationToken
    ) {
        if (message == null) {
            throw new ArgumentNullException(nameof(message));
        }

        var result = Channel.CreateUnbounded<PongMessage>();

        result.Writer.RunBackgroundOperation(async (ch, ct) => {
            result.Writer.TryWrite(new PongMessage() {
                CorrelationId = message.CorrelationId
            });
        }, true, BackgroundTaskService, cancellationToken);

        return Task.FromResult(result.Reader);
    }

    // DuplexStream
    [ExtensionFeatureOperation(
        Name = "Ping",
        Description = "Performs a duplex streaming ping operation on the adapter.",
        InputParameterDescription = "The ping messages.",
        OutputParameterDescription = "The pong messages."
    )]
    public Task<ChannelReader<PongMessage>> Ping(
        ChannelReader<PingMessage> channel,
        CancellationToken cancellationToken
    ) {
        if (channel == null) {
            throw new ArgumentNullException(nameof(channel));
        }

        var result = Channel.CreateUnbounded<PongMessage>();

        result.Writer.RunBackgroundOperation(async (ch, ct) => {
            await foreach (var message in channel.ReadAllAsync(ct).ConfigureAwait(false)) {
                if (message == null) {
                    continue;
                }

                result.Writer.TryWrite(new PongMessage() {
                    CorrelationId = message.CorrelationId
                });
            }
        }, true, BackgroundTaskService, cancellationToken);

        return Task.FromResult(result.Reader);
    }

}

public class PingMessage {
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}

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
  "operationId": "asc:extensions/example/ping-pong/Ping/Invoke/",
  "operationType": "Invoke",
  "name": "Ping",
  "description": "Performs a ping operation on the adapter.",
  "input": {
    "description": "The ping message.",
    "exampleValue": "{ \"CorrelationId\": \"310b5036-9956-40c0-872a-59a68bc13a8f\" }"
  },
  "output": {
    "description": "The pong message.",
    "exampleValue": "{ \"CorrelationId\": \"10085252-5b69-4ca2-a727-c850c7825630\" }"
  }
}
```
