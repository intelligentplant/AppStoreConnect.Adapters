# Tag History Polling

Polling tags for historical data values is implemented by a number of different features, with each feature handling a particular type of historical query:

| Feature | Description |
| ------- | ----------- |
| [IReadRawTagValues](../../src/DataCore.Adapter.Abstractions/RealTimeData/IReadRawTagValues.cs) | Allows polling of raw, unprocessed historical tag values. |
| [IReadProcessedTagValues](../../src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs) | Provides methods to request aggregated tag values (such as the average value of a tag over an hourly interval), and for discovering the aggregations that the adapter supports. |
| [IReadPlotTagValues](../../src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) | Allows retrieval of a best-fit curve of tag values over a given time period and granularity, for displaying in a chart. |
| [IReadTagValuesAtTimes](../../src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs) | Allows retrieval of tag values at specific moments in time. |

Industrial data sources such as OPC UA servers usually implement an equivalent of most or all of the above query types. When writing an adapter to connect to such a system, implementation usually consists of mapping from the adapter query to the equivalent native query in the data source.


# Using ReadHistoricalTagValues to implement historical queries

When connecting to a system that only supports the retrieval of raw historical values (i.e. the adapter implements the `IReadRawTagValues` feature), the [ReadHistoricalTagValues](../../src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) helper class can be used create shim implementations of the other historical polling features. An example of where this might be used is in an adapter that reads its raw data from a CSV file, or from a SQL database.

> For performance reasons, native implementations of historical polling features should always be used if possible. In order to function, `ReadHistoricalTagValues` must always retrieve raw history and then perform in-memory aggregation or filtering.  

`ReadHistoricalTagValues` require an `IReadRawTagValues` and an `ITagInfo` feature to be provided by the adapter. The easiest way to create a `ReadHistoricalTagValues` instance is to use the static `ReadHistoricalTagValues.ForAdapter(IAdapter)` method:

```cs
public class MyAdapter : AdapterBase<MyAdapterOptions> {
    
    public MyAdapter(
        string id,
        IOptions<MyAdapterOptions> options,
        IBackgroundTaskService backgroundTaskService,
        ILoggerFactory loggerFactory
    ) : base(id, options, backgroundTaskService, loggerFactory) {
        // TODO: Implement the ITagInfo and IReadRawTagValues features or delegate them to external providers.

        AddFeatures(ReadHistoricalTagValues.ForAdapter(this));
    }

}
```


# Aggregation and interpolation helper classes

`ReadHistoricalTagValues` makes use of the following helper classes to provide aggregation and interpolation functionality:

| Helper Class | Description |
| ------------ | ----------- |
| [InterpolationHelper](../../src/DataCore.Adapter/RealTimeData/Utilities/InterpolationHelper.cs) | Provides methods for interpolating or extrapolating tag values at fixed sample intervals or at specific sample times. |
| [AggregationHelper](../../src/DataCore.Adapter/RealTimeData/Utilities/AggregationHelper.cs) | Provides methods for calculating common time series aggregations, and for registering and calling custom aggregation functions. |
| [PlotHelper](../../src/DataCore.Adapter/RealTimeData/Utilities/PlotHelper.cs) | Provides methods for selecting a best-fit subset of values from a raw data set. |

If you do not need or want to implement all of the features supplied by `ReadHistoricalTagValues`, you can also write your own implementations using the above helpers.
