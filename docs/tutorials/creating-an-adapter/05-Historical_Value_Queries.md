# Tutorial - Creating an Adapter

_This is part 5 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Historical Value Queries

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-05)._

At this point, we have an adapter that allows callers to browse the available measurements, poll them, and subscribe to receive value changes in real-time. These are already useful features (and in some cases, are the extent of what e.g. an IoT device can provide us with). However, in many cases, we need to interface with systems that also allow us to request the value of a tag over a time range. In some cases, we might only be able to ask for the raw tag values (that is, the values that have been recorded in a database, a CSV file, or some other source). In other cases (e.g. when we are connecting to an industrial plant historian such as OSIsoft PI, or an OPC UA server), we might be able to ask the external source to compute aggregated values for tags, such as the average value of a tag over each hour in the previous 24 hour time period.

Adapters can implement several features related to historical data queries, namely:

- [IReadRawTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadRawTagValues.cs) - for reading raw, unprocessed historical values.
- [IReadPlotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadPlotTagValues.cs) - for reading values that will be visualized on e.g. a line chart. Implementations of this feature will typically perform some sort of selection algorithm to return meaningful values over a query time range.
- [IReadTagValuesAtTimes](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadTagValuesAtTimes.cs) - for requesting the values of tags at specific historical sample times.
- [IReadProcessedTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadProcessedTagValues.cs) - for requesting aggregated or interpolated tag values at fixed sample intervals.

If you are interfacing with an industrial plant historian, the historian may already implement most of these capabilities; consult the vendor's API documention for details. 

To start off, we will update our adapter class to declare that it implements `IReadRawTagValues`:

```csharp
public class Adapter : AdapterBase, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues {
    // -- snip --
}
```

Next, we will implement the `ReadRawTagValues` method:

```csharp
public ChannelReader<TagValueQueryResult> ReadRawTagValues(
    IAdapterCallContext context, 
    ReadRawTagValuesRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);
    var result = Channel.CreateUnbounded<TagValueQueryResult>();

    TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
        foreach (var tag in request.Tags) {
            if (ct.IsCancellationRequested) {
                break;
            }
            if (string.IsNullOrWhiteSpace(tag)) {
                continue;
            }
            if (!_tagsById.TryGetValue(tag, out var t) && !_tagsByName.TryGetValue(tag, out t)) {
                continue;
            }

            var sampleCount = 0;
            var ts = CalculateSampleTime(request.UtcStartTime).AddSeconds(-1);
            var rnd = new Random(ts.GetHashCode());

            do {
                ts = ts.AddSeconds(1);
                if (request.BoundaryType == RawDataBoundaryType.Inside && (ts < request.UtcStartTime || ts > request.UtcEndTime)) {
                    continue;
                }
                ch.TryWrite(CalculateValueForTag(t, ts, rnd.NextDouble() < 0.9 ? TagValueStatus.Good : TagValueStatus.Bad));
            } while (ts < request.UtcEndTime && (request.SampleCount < 1 || sampleCount <= request.SampleCount));
        }
        
    }, result.Writer, true, cancellationToken);

    return result;
}
```

Let's take a closer look at the part of the method that emits values for a given tag (variable `t`):

```csharp
var sampleCount = 0;
```

We use the `sampleCount` variable to keep track of the number of samples we have emitted for each tag, because the caller can optionally limit the maximum number of samples they want to retrieve for each tag.

```csharp
var ts = CalculateSampleTime(request.UtcStartTime).AddSeconds(-1);
var rnd = new Random(ts.GetHashCode());

do {
    ts = ts.AddSeconds(1);
    if (request.BoundaryType == RawDataBoundaryType.Inside && (ts < request.UtcStartTime || ts > request.UtcEndTime)) {
        continue;
    }
    ch.TryWrite(CalculateValueForTag(t, ts, rnd.NextDouble() < 0.9 ? TagValueStatus.Good : TagValueStatus.Bad));
} while (ts < request.UtcEndTime && (request.SampleCount < 1 || sampleCount <= request.SampleCount));
```

The `ReadRawTagValuesRequest` class includes a `BoundaryType` property, which a caller can use to specify if they only want raw values falling inside the query time range, or if they also want to receive the raw values immediately before and immediately after the boundary times. In our `do..while` loop, we increment the sample time by one second and then check to see if we should skip the current sample time based on the query's boundary type. We calculate and emit the value if required, and then repeat until we reach the query end time, or we retrieve the maximum sample count specified by the caller. For each sample, we randomly decide if the quality status of the sample should be good or bad in the same way as in `ReadSnapshotTagValues`; this will have an effect on how aggregated values are calculated (see below).

At this point, we have added the ability to ask for raw historical values from our adapter, but we have not implemented the other historical query features (`IReadPlotTagValues`, `IReadTagValuesAtTimes`, and `IReadProcessedTagValues`). We could implement these features ourselves - this would be a good idea if we were connecting to an underlying source that supported them - but we also have a second option: since we are using the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package, we can take advantage of the [ReadHistoricalTagValues](/src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) helper class.

The `ReadHistoricalTagValues` class provides implementations of the remaining historical query features for any adapter that implements the `ITagInfo` and `IReadRawTagValues` features. The implementation relies on retrieving raw tag values as part of every historical query and then transforming them. Due to the extensive use of `System.Threading.Channels.Channel<T>` in adapter features, this can be done without requiring an extensive memory overhead, but a native implementation would always be expected to perform better, since the computation of values is done by the source, rather than having to retrieve potentially large numbers of raw values in order to perform the calculation inside the adapter itself.

Registering `ReadHistoricalTagValues` is a simple change to our adapter's constructor:

```csharp
public Adapter(
    string id,
    string name,
    string description = null,
    IBackgroundTaskService scheduler = null,
    ILogger<Adapter> logger = null
) : base(
    id, 
    name, 
    description, 
    scheduler, 
    logger
) {
    AddFeature<ISnapshotTagValuePush, PollingSnapshotTagValuePush>(PollingSnapshotTagValuePush.ForAdapter(
        this, 
        TimeSpan.FromSeconds(5)
    ));

    AddFeatures(ReadHistoricalTagValues.ForAdapter(this));
}
```

Note that, in this case, we are using the `AddFeatures` method inherited from `AdapterBase` to automatically scan the `ReadHistoricalTagValues` object for adapter features and register them with our own adapter.

### A Note on Data Function Descriptors

The `IReadProcessedTagValues` feature consists of two parts: the `ReadProcessedTagValues` method performs the actual data queries, and the `GetSupportedDataFunctions` method returns information about what sort of aggregation is supported by the adapter. The `GetSupportedDataFunctions` method returns [DataFunctionDescriptor](/src/DataCore.Adapter.Core/RealTimeData/DataFunctionDescriptor.cs) objects that describe the available aggregates. The [DefaultDataFunctions](/src/DataCore.Adapter.Abstractions/RealTimeData/DefaultDataFunctions.cs) class defines commonly-implemented data functions that can be re-used in compatible adapters. 

The `ReadHistoricalTagValues` class implements all functions defined in `DefaultDataFunctions`; it is also possible to define custom aggregate functions and add them to a `ReadHistoricalTagValues` instance using its `RegisterDataFunction` method.


## Testing

Modify the `Run` method in `Program.cs` as follows:

```csharp
private static async Task Run(IAdapterCallContext context, CancellationToken cancellationToken) {
    await using (IAdapter adapter = new Adapter(AdapterId, AdapterDisplayName, AdapterDescription)) {

        await adapter.StartAsync(cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"[{adapter.Descriptor.Id}]");
        Console.WriteLine($"  Name: {adapter.Descriptor.Name}");
        Console.WriteLine($"  Description: {adapter.Descriptor.Description}");
        Console.WriteLine("  Properties:");
        foreach (var prop in adapter.Properties) {
            Console.WriteLine($"    - {prop.Name} = {prop.Value}");
        }
        Console.WriteLine("  Features:");
        foreach (var feature in adapter.Features.Keys) {
            Console.WriteLine($"    - {feature.Name}");
        }

        var tagSearchFeature = adapter.GetFeature<ITagSearch>();
        var readRawFeature = adapter.GetFeature<IReadRawTagValues>();
        var readProcessedFeature = adapter.GetFeature<IReadProcessedTagValues>();

        Console.WriteLine();
        Console.WriteLine("  Supported Aggregations:");
        var funcs = new List<DataFunctionDescriptor>();
        await foreach (var func in readProcessedFeature.GetSupportedDataFunctions(context, cancellationToken).ReadAllAsync()) {
            funcs.Add(func);
            Console.WriteLine($"    - {func.Id}");
            Console.WriteLine($"      - Name: {func.Name}");
            Console.WriteLine($"      - Description: {func.Description}");
        }

        var tags = tagSearchFeature.FindTags(
            context,
            new FindTagsRequest() {
                Name = "Sin*",
                PageSize = 1
            },
            cancellationToken
        );

        await tags.WaitToReadAsync(cancellationToken);
        tags.TryRead(out var tag);

        Console.WriteLine();
        Console.WriteLine("[Tag Details]");
        Console.WriteLine($"  Name: {tag.Name}");
        Console.WriteLine($"  ID: {tag.Id}");
        Console.WriteLine($"  Description: {tag.Description}");
        Console.WriteLine("  Properties:");
        foreach (var prop in tag.Properties) {
            Console.WriteLine($"    - {prop.Name} = {prop.Value}");
        }

        var now = DateTime.UtcNow;
        var start = now.AddSeconds(-15);
        var end = now;
        var sampleInterval = TimeSpan.FromSeconds(5);

        Console.WriteLine();
        Console.WriteLine($"  Raw Values ({start:HH:mm:ss.fff} - {end:HH:mm:ss.fff} UTC):");
        var rawValues = readRawFeature.ReadRawTagValues(
            context,
            new ReadRawTagValuesRequest() {
                Tags = new[] { tag.Id },
                UtcStartTime = start,
                UtcEndTime = end,
                BoundaryType = RawDataBoundaryType.Outside
            },
            cancellationToken
        );
        await foreach (var value in rawValues.ReadAllAsync(cancellationToken)) {
            Console.WriteLine($"    - {value.Value}");
        }

        foreach (var func in funcs) {
            Console.WriteLine();
            Console.WriteLine($"  {func.Name} Values ({sampleInterval} sample interval):");

            var processedValues = readProcessedFeature.ReadProcessedTagValues(
                context,
                new ReadProcessedTagValuesRequest() {
                    Tags = new[] { tag.Id },
                    DataFunctions = new[] { func.Id },
                    UtcStartTime = start,
                    UtcEndTime = end,
                    SampleInterval = sampleInterval
                },
                cancellationToken
            );

            await foreach (var value in processedValues.ReadAllAsync(cancellationToken)) {
                Console.WriteLine($"    - {value.Value}");
            }
        }
    }
}
```

After displaying the usual adapter information, the method will now display the ID, name and description of each supported aggregate function (that is, each data function supported by the `ReadHistoricalTagValues` class), retrieve a single tag, and then request raw historical data for the last 15 seconds, followed by aggregated data for each data function for the same time range, using a sample interval of 5 seconds. When you run the program, you should see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-03-16T10:44:02Z
  Features:
    - IHealthCheck
    - IReadSnapshotTagValues
    - ITagSearch
    - ISnapshotTagValuePush
    - IReadTagValuesAtTimes
    - IReadPlotTagValues
    - ITagInfo
    - IReadProcessedTagValues
    - IReadRawTagValues

  Supported Aggregations:
    - AVG
      - Name: Average
      - Description: Average value calculated over sample interval.
      - Properties:
        - Timestamp = Start of Interval
    - COUNT
      - Name: Count
      - Description: The number of good-quality raw samples that have been recorded for the tag at each sample interval.
      - Properties:
        - Timestamp = Start of Interval
    - INTERP
      - Name: Interpolated
      - Description: Interpolates a value at each sample interval based on the raw values on either side of the sample time for the interval.
      - Properties:
        - Timestamp = Start of Interval/End of Interval
    - MAX
      - Name: Maximum
      - Description: Maximum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the maximum value occurred at.
      - Properties:
        - Timestamp = Timestamp of Maximum Value
    - MIN
      - Name: Minimum
      - Description: Minimum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the minimum value occurred at.
      - Properties:
        - Timestamp = Timestamp of Minimum Value
    - PERCENTBAD
      - Name: Percent Bad
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have bad-quality status.
      - Properties:
        - Timestamp = Start of Interval
    - PERCENTGOOD
      - Name: Percent Good
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have good-quality status.
      - Properties:
        - Timestamp = Start of Interval
    - RANGE
      - Name: Range
      - Description: The difference between the minimum good-quality value and maximum good-quality value in each sample interval.
      - Properties:
        - Timestamp = Start of Interval
    - DELTA
      - Name: Delta
      - Description: The difference between the earliest good-quality value and latest good-quality value in each sample interval.
      - Properties:
        - Timestamp = Start of Interval

[Tag Details]
  Name: Sinusoid_Wave
  ID: 1
  Description: A tag that returns a sinusoid wave value
  Properties:
    - Wave Type = Sinusoid

  Raw Values (10:43:47.916 - 10:44:02.916 UTC):
    - -0.97814760073386076 @ 2020-03-16T10:43:47.0000000Z [Bad Quality]
    - -0.95105651629521326 @ 2020-03-16T10:43:48.0000000Z [Good Quality]
    - -0.91354545764265038 @ 2020-03-16T10:43:49.0000000Z [Good Quality]
    - -0.86602540378469095 @ 2020-03-16T10:43:50.0000000Z [Good Quality]
    - -0.80901699437520191 @ 2020-03-16T10:43:51.0000000Z [Bad Quality]
    - -0.74314482547763605 @ 2020-03-16T10:43:52.0000000Z [Good Quality]
    - -0.66913060635907351 @ 2020-03-16T10:43:53.0000000Z [Good Quality]
    - -0.58778525229264955 @ 2020-03-16T10:43:54.0000000Z [Good Quality]
    - -0.50000000000012679 @ 2020-03-16T10:43:55.0000000Z [Good Quality]
    - -0.40673664307628399 @ 2020-03-16T10:43:56.0000000Z [Good Quality]
    - -0.30901699437538294 @ 2020-03-16T10:43:57.0000000Z [Good Quality]
    - -0.20791169081813718 @ 2020-03-16T10:43:58.0000000Z [Good Quality]
    - -0.10452846326796639 @ 2020-03-16T10:43:59.0000000Z [Good Quality]
    - -2.429996360213911E-13 @ 2020-03-16T10:44:00.0000000Z [Good Quality]
    - 0.10452846326748305 @ 2020-03-16T10:44:01.0000000Z [Good Quality]
    - 0.207911690817217 @ 2020-03-16T10:44:02.0000000Z [Good Quality]
    - 0.30901699437448821 @ 2020-03-16T10:44:03.0000000Z [Good Quality]

  Average Values (00:00:05 sample interval):
    - -0.86844305080004769 @ 2020-03-16T10:43:47.9169319Z [Uncertain Quality]
    - -0.49453389922070334 @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - -3.2930325133406767E-13 @ 2020-03-16T10:43:57.9169319Z [Good Quality]

  Count Values (00:00:05 sample interval):
    - 4 @ 2020-03-16T10:43:47.9169319Z [Uncertain Quality]
    - 5 @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - 5 @ 2020-03-16T10:43:57.9169319Z [Good Quality]

  Interpolated Values (00:00:05 sample interval):
    - -0.95330692120647131 @ 2020-03-16T10:43:47.9169319Z [Uncertain Quality]
    - -0.67527882691423613 @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - -0.21631031628456082 @ 2020-03-16T10:43:57.9169319Z [Good Quality]
    - 0.3027070700825269 @ 2020-03-16T10:44:02.9169319Z [Good Quality]

  Maximum Values (00:00:05 sample interval):
    - -0.74314482547763605 @ 2020-03-16T10:43:52.0000000Z [Uncertain Quality]
    - -0.30901699437538294 @ 2020-03-16T10:43:57.0000000Z [Good Quality]
    - 0.207911690817217 @ 2020-03-16T10:44:02.0000000Z [Good Quality]

  Minimum Values (00:00:05 sample interval):
    - -0.95105651629521326 @ 2020-03-16T10:43:48.0000000Z [Uncertain Quality]
    - -0.66913060635907351 @ 2020-03-16T10:43:53.0000000Z [Good Quality]
    - -0.20791169081813718 @ 2020-03-16T10:43:58.0000000Z [Good Quality]

  Percent Bad Values (00:00:05 sample interval):
    - 20 % @ 2020-03-16T10:43:47.9169319Z [Good Quality]
    - 0 % @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - 0 % @ 2020-03-16T10:43:57.9169319Z [Good Quality]

  Percent Good Values (00:00:05 sample interval):
    - 80 % @ 2020-03-16T10:43:47.9169319Z [Good Quality]
    - 100 % @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - 100 % @ 2020-03-16T10:43:57.9169319Z [Good Quality]

  Range Values (00:00:05 sample interval):
    - 0.20791169081757721 @ 2020-03-16T10:43:47.9169319Z [Uncertain Quality]
    - 0.36011361198369057 @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - 0.41582338163535415 @ 2020-03-16T10:43:57.9169319Z [Good Quality]

  Delta Values (00:00:05 sample interval):
    - 0.20791169081757721 @ 2020-03-16T10:43:47.9169319Z [Uncertain Quality]
    - 0.36011361198369057 @ 2020-03-16T10:43:52.9169319Z [Good Quality]
    - 0.41582338163535415 @ 2020-03-16T10:43:57.9169319Z [Good Quality]
```

Note that the output of several functions contains values with `Uncertain` quality. A lot of the built-in aggregates only operate on good-quality values, and will return `Uncertain` for the quality status if any values in the time interval they operated on are non-good quality.


## Next Steps

In the [next chapter](06-Adapter_Configuration_Options.md), we will modify our adapter to accept a set of configurable options, instead of hard-coding its settings.
