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

Whenever the health status of your adaper changes (e.g. you become disconnected from an external service that the adapter relies on), you should call the `OnHealthStatusChanged` method from your implementation. This will recompute the overall health status of the adapter and push the update to any subscribers to the `IHealthCheck` feature.


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
