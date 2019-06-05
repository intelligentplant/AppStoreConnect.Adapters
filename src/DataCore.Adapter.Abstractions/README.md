# DataCore.Adapter.Abstractions

This project contains core types and abstractions for implementing an App Store Connect Adapter. An adapter is a component that exposes real-time process data and/or alarm & event data to App Store Connect. This data can then be used by apps on the [Industrial App Store](https://appstore.intelligentplant.com) such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).


# Implementing an Adapter

All adapters implement the [IAdapter](./IAdapter.cs) interface. Each adapter implements a set of *features*, which are exposed via an [IAdapterFeaturesCollection](./IAdapterFeaturesCollection.cs). Individual features are defined as interfaces, and must inherit from [IAdapterFeature](./IAdapterFeature.cs). The [AdapterFeaturesCollection](./AdapterFeaturesCollection.cs) class provides a default implementation of `IAdapterFeaturesCollection` that can register and unregister features dynamically at runtime.

Adapter implementers can pick and choose which features they want to provide. For example, the `DataCore.Adapter.RealTimeData.Features` namespace defines interfaces for features related to real-time process data (searching for available tags, requesting snapshot tag values, performing various types of historical data queries, and so on). An individual adapter can implement features related to process data, alarm and event sources, and alarm and event sinks, as required.


## Helper Classes

The [DataCore.Adapter](/src/DataCore.Adapter.Utilities) project contains a number of helper classes to simplify adapter implementation. For example, if an adapter only natively supports the retrieval of [raw, unprocessed tag values](./RealTimeData/Features/IReadRawTagValues.cs), the [ReadHistoricalTagValuesHelper](/src/DataCore.Adapter/DataSource/Utilities/ReadHistoricalTagValuesHelper.cs) class can be used to provide support for [interpolated](./RealTimeData/Features/IReadInterpolatedTagValues.cs), [values-at-times](./RealTimeData/Features/IReadTagValuesAtTimes.cs), [plot](./RealTimeData/Features/IReadPlotTagValues.cs), and [aggregated](./RealTimeData/Features/IReadProcessedTagValues.cs) data queries.

Adapter features make extensive use of the [System.Threading.Channels](https://www.nuget.org/packages/System.Threading.Channels/) NuGet package, to allow query results to be streamed back to the caller asynchronously. The [DataCore.Adapter](/src/DataCore.Adapter) project also contains extension methods for the `ChannelReader<T>` and `ChannelWriter<T>` and classes, to easily read from or write to channels in background tasks:

```csharp
ChannelReader<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
    var channel = Channel.CreateUnbounded<TagValueQueryResult>()

    channel.Writer.RunBackgroundOperation(async (ch, ct) => {
        using (var values = GetSnapshotValues(requestTags)) {
            while (await values.MoveNext(ct).ConfigureAwait)) {
                var canWrite = await channel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false);
                if (!canWrite) {
                    return;
                }

                channel.Writer.TryWrite(values.Current);
            }
        }
    }, true, cancellationToken);

    return channel.Reader;
}

private IAsyncEnumerator<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags) {
    ...
}
```

# Implementing Authorization

All calls to adapter features include information about the caller (via the [IAdapterCallContext](./IAdapterCallContext.cs) interface).

Alternatively, you can implement the [IAdapterAuthorizationService](./IAdapterAuthorizationService.cs) and use it in your host application to pre-authorize access to individual adapters and features.