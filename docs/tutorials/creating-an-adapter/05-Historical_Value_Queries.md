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
public Task<ChannelReader<TagValueQueryResult>> ReadRawTagValues(
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

    return Task.FromResult(result.Reader);
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

At this point, we have added the ability to ask for raw historical values from our adapter, but we have not implemented the other historical query features (`IReadPlotTagValues`, `IReadTagValuesAtTimes`, and `IReadProcessedTagValues`). We could implement these features ourselves - this would be a good idea if we were connecting to an underlying source that natively supported them - but we also have a second option: since we are using the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package, we can take advantage of the [ReadHistoricalTagValues](/src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) helper class.

The `ReadHistoricalTagValues` class provides implementations of the remaining historical query features for any adapter that implements the `ITagInfo` and `IReadRawTagValues` features. The implementation relies on retrieving raw tag values as part of every historical query and then transforming them. Due to the extensive use of `System.Threading.Channels.Channel<T>` in adapter features, this can be done without requiring an extensive memory overhead, but a native implementation would always be expected to perform better, since the computation of values is done by the source, rather than having to retrieve potentially large numbers of raw values in order to perform the calculation inside the adapter itself.

Registering `ReadHistoricalTagValues` is a simple change to our adapter's constructor:

```csharp
public Adapter(
    string id,
    string name,
    string description = null,
    IBackgroundTaskService backgroundTaskService = null,
    ILogger<Adapter> logger = null
) : base(
    id, 
    name, 
    description, 
    backgroundTaskService, 
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
            Console.WriteLine($"    - {feature}");
        }

        var tagSearchFeature = adapter.GetFeature<ITagSearch>();
        var readRawFeature = adapter.GetFeature<IReadRawTagValues>();
        var readProcessedFeature = adapter.GetFeature<IReadProcessedTagValues>();

        Console.WriteLine();
        Console.WriteLine("  Supported Aggregations:");
        var funcs = new List<DataFunctionDescriptor>();
        await foreach (var func in (await readProcessedFeature.GetSupportedDataFunctions(context, cancellationToken)).ReadAllAsync()) {
            funcs.Add(func);
            Console.WriteLine($"    - {func.Id}");
            Console.WriteLine($"      - Name: {func.Name}");
            Console.WriteLine($"      - Description: {func.Description}");
            Console.WriteLine("      - Properties:");
            foreach (var prop in func.Properties) {
                Console.WriteLine($"        - {prop.Name} = {prop.Value}");
            }
        }

        var tags = await tagSearchFeature.FindTags(
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
        var rawValues = await readRawFeature.ReadRawTagValues(
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

            var processedValues = await readProcessedFeature.ReadProcessedTagValues(
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
    - Startup Time = 2020-09-18T10:00:48Z
  Features:
    - asc:features/diagnostics/health-check/
    - asc:features/real-time-data/values/read/processed/
    - asc:features/real-time-data/values/push/
    - asc:features/real-time-data/values/read/at-times/
    - asc:features/real-time-data/values/read/raw/
    - asc:features/real-time-data/tags/search/
    - asc:features/real-time-data/values/read/snapshot/
    - asc:features/real-time-data/tags/info/
    - asc:features/real-time-data/values/read/plot/

  Supported Aggregations:
    - AVG
      - Name: Average
      - Description: Average value calculated over sample interval.
      - Properties:
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - COUNT
      - Name: Count
      - Description: The number of good-quality raw samples that have been recorded for the tag at each sample interval.
      - Properties:
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - INTERP
      - Name: Interpolated
      - Description: Interpolates a value at each sample interval based on the raw values on either side of the sample time for the interval.
      - Properties:
        - Status Calculation = Worst-case status of the samples used in the calculation.
    - MAX
      - Name: Maximum
      - Description: Maximum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the maximum value occurred at.
      - Properties:
        - Timestamp Calculation = Timestamp of maximum value
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - MIN
      - Name: Minimum
      - Description: Minimum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the minimum value occurred at.
      - Properties:
        - Timestamp Calculation = Timestamp of minimum value
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - PERCENTBAD
      - Name: Percent Bad
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have bad-quality status.
      - Properties:
        - Status Calculation = Quality status is always "Good".
    - PERCENTGOOD
      - Name: Percent Good
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have good-quality status.
      - Properties:
        - Status Calculation = Quality status is always "Good".
    - RANGE
      - Name: Range
      - Description: The absolute difference between the minimum good-quality value and maximum good-quality value in each sample interval.
      - Properties:
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - DELTA
      - Name: Delta
      - Description: The signed difference between the earliest good-quality value and latest good-quality value in each sample interval.
      - Properties:
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - VARIANCE
      - Name: Variance
      - Description: The variance of all good-quality values in each sample interval
      - Properties:
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.
    - STDDEV
      - Name: Standard Deviation
      - Description: The standard deviation of all good-quality values in each sample interval
      - Properties:
        - Status Calculation = "Uncertain" if any non-good quality values were skipped, or "Good" otherwise.

[Tag Details]
  Name: Sinusoid_Wave
  ID: 1
  Description: A tag that returns a sinusoid wave value
  Properties:
    - Wave Type = Sinusoid

  Raw Values (10:00:33.386 - 10:00:48.386 UTC):
    - -0.30901699437483965 @ 2020-09-18T10:00:33.0000000Z [Good Quality]
    - -0.40673664307534668 @ 2020-09-18T10:00:34.0000000Z [Good Quality]
    - -0.49999999999963213 @ 2020-09-18T10:00:35.0000000Z [Good Quality]
    - -0.58778525229218737 @ 2020-09-18T10:00:36.0000000Z [Good Quality]
    - -0.66913060635864896 @ 2020-09-18T10:00:37.0000000Z [Good Quality]
    - -0.7431448254772538 @ 2020-09-18T10:00:38.0000000Z [Good Quality]
    - -0.80901699437486618 @ 2020-09-18T10:00:39.0000000Z [Good Quality]
    - -0.86602540378417803 @ 2020-09-18T10:00:40.0000000Z [Good Quality]
    - -0.91354545764241801 @ 2020-09-18T10:00:41.0000000Z [Good Quality]
    - -0.95105651629503674 @ 2020-09-18T10:00:42.0000000Z [Good Quality]
    - -0.97814760073374196 @ 2020-09-18T10:00:43.0000000Z [Good Quality]
    - -0.99452189536824875 @ 2020-09-18T10:00:44.0000000Z [Good Quality]
    - -1 @ 2020-09-18T10:00:45.0000000Z [Good Quality]
    - -0.99452189536828295 @ 2020-09-18T10:00:46.0000000Z [Good Quality]
    - -0.97814760073390428 @ 2020-09-18T10:00:47.0000000Z [Good Quality]
    - -0.9510565162952781 @ 2020-09-18T10:00:48.0000000Z [Bad Quality]
    - -0.91354545764273565 @ 2020-09-18T10:00:49.0000000Z [Good Quality]

  Average Values (00:00:05 sample interval):
    - -0.58135946544061379 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - -0.90355839456604825 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - -0.99179784786760894 @ 2020-09-18T10:00:43.3867336Z [Uncertain Quality]

  Count Values (00:00:05 sample interval):
    - 5 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 5 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - 4 @ 2020-09-18T10:00:43.3867336Z [Uncertain Quality]

  Interpolated Values (00:00:05 sample interval):
    - -0.3468090131375548 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - -0.76862017537898131 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - -0.98448018234125545 @ 2020-09-18T10:00:43.3867336Z [Good Quality]
    - -0.95544072449206174 @ 2020-09-18T10:00:48.3867336Z [Uncertain Quality]

  Maximum Values (00:00:05 sample interval):
    - -0.40673664307534668 @ 2020-09-18T10:00:34.0000000Z [Good Quality]
    - -0.80901699437486618 @ 2020-09-18T10:00:39.0000000Z [Good Quality]
    - -0.97814760073390428 @ 2020-09-18T10:00:47.0000000Z [Uncertain Quality]

  Minimum Values (00:00:05 sample interval):
    - -0.7431448254772538 @ 2020-09-18T10:00:38.0000000Z [Good Quality]
    - -0.97814760073374196 @ 2020-09-18T10:00:43.0000000Z [Good Quality]
    - -1 @ 2020-09-18T10:00:45.0000000Z [Uncertain Quality]

  Percent Bad Values (00:00:05 sample interval):
    - 0 % @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 0 % @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - 20 % @ 2020-09-18T10:00:43.3867336Z [Good Quality]

  Percent Good Values (00:00:05 sample interval):
    - 100 % @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 100 % @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - 80 % @ 2020-09-18T10:00:43.3867336Z [Good Quality]

  Range Values (00:00:05 sample interval):
    - 0.33640818240190712 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 0.16913060635887578 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - 0.021852399266095723 @ 2020-09-18T10:00:43.3867336Z [Uncertain Quality]

  Delta Values (00:00:05 sample interval):
    - 0.33640818240190712 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 0.16913060635887578 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - -0.016374294634344477 @ 2020-09-18T10:00:43.3867336Z [Uncertain Quality]

  Variance Values (00:00:05 sample interval):
    - 0.01775801483613865 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 0.0045665411051352776 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - 8.9481805328589981E-05 @ 2020-09-18T10:00:43.3867336Z [Uncertain Quality]

  Standard Deviation Values (00:00:05 sample interval):
    - 0.13325920169406183 @ 2020-09-18T10:00:33.3867336Z [Good Quality]
    - 0.067576187411952135 @ 2020-09-18T10:00:38.3867336Z [Good Quality]
    - 0.0094594822970704897 @ 2020-09-18T10:00:43.3867336Z [Uncertain Quality]
```

Note that the output of several functions contains values with `Uncertain` quality. A lot of the built-in aggregates only operate on good-quality values, and will return `Uncertain` for the quality status if any values in the time interval they operated on are non-good quality.


## Next Steps

In the [next chapter](06-Adapter_Configuration_Options.md), we will modify our adapter to accept a set of configurable options, instead of hard-coding its settings.
