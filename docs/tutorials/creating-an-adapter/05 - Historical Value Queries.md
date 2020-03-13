# Tutorial - Creating an Adapter

_This is part 5 of a tutorial series about creating an adapter. The introduction to the series can be found [here](./00 - Introduction.md)_


## Historical Value Queries

At this point, we have an adapter that allows callers to browse the available measurements, poll them, and subscribe to receive value changes in real-time. These are already useful features (and in some cases, are the extent of what e.g. an IoT device can provide us with). However, in many cases, we need to interface with systems that also allow us to request the value of a tag over a time range. In some cases, we might only be able to ask for the raw tag values (that is, the values that have been recorded in a database, a CSV file, or some other source). In other cases (e.g. when we are connecting to an industrial plant historian such as OSIsoft PI, or an OPC UA server), we might be able to ask the external source to compute aggregated values for tags, such as the average value of a tag over each hour in the previous 24 hour time period.

Adapters can implement several features related to historical data queries, namely:

- `IReadRawTagValues` - for reading raw, unprocessed historical values.
- `IReadPlotTagValues` - for reading values that will be visualized on e.g. a line chart. Implementations of this feature will typically perform some sort of selection algorithm to return meaningful values over a query time range.
- `IReadTagValuesAtTimes` - for requesting the values of tags at specific historical sample times.
- `IReadProcessedTagValues` - for requesting aggregated or interpolated tag values at fixed sample intervals.

If you are interfacing with an industrial plant historian, the historian may already implement most of these capabilities; consult the vendor's API documention for details. 

To start off, we will update our adapter class to declare that it implements `IReadRawTagValues`:

```csharp
public class Adapter : AdapterBase, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues {
    // -- snip --
}
```

Up until now, we have been generating completely random values for our tags every time we poll them. However, it would be helpful now to generate idempotent result sets (that is, the same query should return the same result every time). In order to do this, we'll start by adding a helper method to our adapter that, for a given tag and query start time, will return us a `System.Random` object that will always be created using the same seed value:

```csharp
private Random GetRng(TagDefinition tag, DateTime startAt) {
    return new Random((tag.GetHashCode() + startAt.GetHashCode()).GetHashCode());
}
```

Next, we will implement the `IReadRawTagValues.ReadRawTagValues` method:

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

            var intervalRnd = GetRng(t, request.UtcStartTime);
            var sampleCount = 0;

            for (var ts = request.UtcStartTime; ts <= request.UtcEndTime && (request.SampleCount < 1 || sampleCount <= request.SampleCount); ts = ts.AddSeconds(intervalRnd.Next(3, 9))) {
                var rnd = GetRng(t, ts);
                ch.TryWrite(new TagValueQueryResult(
                    t.Id,
                    t.Name,
                    TagValueBuilder
                        .Create()
                        .WithUtcSampleTime(ts)
                        .WithValue(rnd.NextDouble())
                        .WithStatus(rnd.NextDouble() <= 0.7 ? TagValueStatus.Good : TagValueStatus.Bad)
                        .Build()
                ));
            }
        }
        
    }, result.Writer, true, cancellationToken);

    return result;
}
```

Let's take a closer look at the part of the method that emits values for a given tag (variable `t`):

```csharp
var intervalRnd = GetRng(t, request.UtcStartTime);
var sampleCount = 0;
```

The `intervalRnd` variable is a `System.Random` that we will use to randomly choose the time interval between raw samples. This is just to allow us to show raw values at slightly irregular times, in the same way that signals from IoT devices or other sources might not arrive at exact intervals. We use the `sampleCount` variable to keep track of the number of samples we have emitted for each tag, because the caller can optionally limit the maximum number of samples they want to retrieve for each tag.

```csharp
for (var ts = request.UtcStartTime; ts <= request.UtcEndTime && (request.SampleCount < 1 || sampleCount <= request.SampleCount); ts = ts.AddSeconds(intervalRnd.Next(3, 9))) {
    var rnd = GetRng(t, ts);
    ch.TryWrite(new TagValueQueryResult(
        t.Id,
        t.Name,
        TagValueBuilder
            .Create()
            .WithUtcSampleTime(ts)
            .WithValue(rnd.NextDouble())
            .WithStatus(rnd.NextDouble() <= 0.7 ? TagValueStatus.Good : TagValueStatus.Bad)
            .Build()
    ));
}
```

In our `for` loop, we move forwards in time from our query start time to query end time, advancing 3-8 seconds every time (the upper boundary of 9 in the call to `System.Random.Next` is exclusive). We stop when we exceed our query end time, or if we emit the maximum number of samples requested by the caller for the tag. At each iteration, we emit a value using the timestamp for our cursor, and we also specify the _quality_ of the value, using the `TagValueStatus` enum. The status allows your adapter to inform the caller if the value is trust-worthy. Typically, an instrument report a non-good status of a value if it detected a fault in the instrument calibration for example.

At this point, we have added the ability to ask for raw historical values from our adapter, but we have not implemented the other historical query features (`IReadPlotTagValues`, `IReadTagValuesAtTimes`, and `IReadProcessedTagValues`). We could implement these features ourselves - this would be a good idea if we were connecting to an underlying source that supported them - but we also have a second option: since we are using the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package, we can take advantage of the `DataCore.Adapter.RealTimeData.ReadHistoricalTagValues` helper class.

The `ReadHistoricalTagValues` provides implementations of the remaining historical query features for any adapter that implements the `ITagInfo` and `IReadRawTagValues` features. The implementation relies on retrieving raw tag values as part of every historical query and then transforming them. Due to the extensive use of `System.Threading.Channels.Channel<T>` in adapter features, this can be done without requiring an extensive memory overhead, but a native implementation would always be expected to perform better, since the computation of values is done by the source, rather than having to retrieve potentially large numbers of raw values in order to perform the calculation inside the adapter itself.

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

The `IReadProcessedTagValues` feature consists of two parts: the `ReadProcessedTagValues` method performs the actual data queries, and the `GetSupportedDataFunctions` method returns information about what sort of aggregation is supported by the adapter. The `GetSupportedDataFunctions` method returns `DataFunctionDescriptor` objects that describe the available aggregates. The `DefaultDataFunctions` class defines commonly-implemented data functions that can be re-used in compatible adapters. 

The `ReadHistoricalTagValues` class implements all functions defined in `DefaultDataFunctions`; it is also possible to define custom aggregate functions and add them to a `ReadHistoricalTagValues` instance using its `RegisterDataFunction` method.


## Testing

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

        var tags = tagSearchFeature.FindTags(
            context,
            new FindTagsRequest() {
                Name = "*",
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

        Console.WriteLine();
        Console.WriteLine("  Supported Aggregations:");
        var funcs = new List<DataFunctionDescriptor>();
        await foreach (var func in readProcessedFeature.GetSupportedDataFunctions(context, cancellationToken).ReadAllAsync()) {
            funcs.Add(func);
            Console.WriteLine($"    - {func.Id}");
            Console.WriteLine($"      - Name: {func.Name}");
            Console.WriteLine($"      - Description: {func.Description}");
        }

        var now = DateTime.UtcNow;

        Console.WriteLine();
        Console.WriteLine("  Raw Values:");
        var rawValues = readRawFeature.ReadRawTagValues(
            context,
            new ReadRawTagValuesRequest() {
                Tags = new[] { tag.Id },
                UtcStartTime = now.AddMinutes(-1),
                UtcEndTime = now
            },
            cancellationToken
        );
        await foreach (var value in rawValues.ReadAllAsync(cancellationToken)) {
            Console.WriteLine($"    - {value.Value.Value} @ {value.Value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss}Z");
        }

        foreach (var func in funcs) {
            Console.WriteLine();
            Console.WriteLine($"  {func.Name} Values:");

            var processedValues = readProcessedFeature.ReadProcessedTagValues(
                context,
                new ReadProcessedTagValuesRequest() { 
                    Tags = new[] { tag.Id },
                    DataFunctions = new[] { func.Id },
                    UtcStartTime = now.AddMinutes(-1),
                    UtcEndTime = now,
                    SampleInterval = TimeSpan.FromSeconds(20)
                },
                cancellationToken
            );

            await foreach (var value in processedValues.ReadAllAsync(cancellationToken)) {
                Console.WriteLine($"    - {value.Value.Value} @ {value.Value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss}Z");
            }
        }
    }
}
```

After displaying the usual adapter information, the method will now display the ID, name and description of each supported aggregate function (that is, each data function supported by the `ReadHistoricalTagValues` class), retrieve a single tag, and then request raw historical data, followed by aggregated data for each data function. When you run the program, you should see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-03-13T14:53:22Z
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
      - Description: Average value calculated over a fixed sample interval.
    - COUNT
      - Name: Count
      - Description: The number of raw samples that have been recorded for the tag over the sample period.
    - INTERP
      - Name: Interpolated
      - Description: Interpolates a value at each sample interval based on the raw values on either side of the sample time for the interval.
    - MAX
      - Name: Maximum
      - Description: Maximum value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the maximum value occurred at.
    - MIN
      - Name: Minimum
      - Description: Minimum value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the minimum value occurred at.
    - PERCENTBAD
      - Name: Percent Bad
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have bad-quality status.
    - PERCENTGOOD
      - Name: Percent Good
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have good-quality status.
    - RANGE
      - Name: Range
      - Description: The difference between the minimum value and maximum value over the sample period.

[Tag Details]
  Name: RandomValue_1
  ID: 1
  Description: A tag that returns a random value
  Properties:
    - MinValue = 0
    - MaxValue = 1

  Raw Values:
    - 0.3663978219807138 @ 2020-03-13T14:52:22Z [Good]
    - 0.24800059304013877 @ 2020-03-13T14:52:27Z [Good]
    - 0.8894961177788191 @ 2020-03-13T14:52:30Z [Bad]
    - 0.5386314590175783 @ 2020-03-13T14:52:35Z [Good]
    - 0.9884358481450173 @ 2020-03-13T14:52:39Z [Good]
    - 0.6318752037509695 @ 2020-03-13T14:52:47Z [Good]
    - 0.8349552787071817 @ 2020-03-13T14:52:50Z [Good]
    - 0.6747278341440148 @ 2020-03-13T14:52:53Z [Good]
    - 0.5977883057658506 @ 2020-03-13T14:52:58Z [Bad]
    - 0.6658352420971893 @ 2020-03-13T14:53:03Z [Good]
    - 0.27325396299048044 @ 2020-03-13T14:53:07Z [Bad]
    - 0.4306695789241556 @ 2020-03-13T14:53:12Z [Good]
    - 0.7912188837263822 @ 2020-03-13T14:53:17Z [Good]

  Average Values:
    - 0.3663978219807138 @ 2020-03-13T14:52:42Z
    - 0.04379721593288575 @ 2020-03-13T14:53:02Z
    - 0.5375642732426358 @ 2020-03-13T14:53:22Z

  Count Values:
    - 4 @ 2020-03-13T14:52:42Z
    - 4 @ 2020-03-13T14:53:02Z
    - 4 @ 2020-03-13T14:53:22Z

  Interpolated Values:
    - 0.3663978219807138 @ 2020-03-13T14:52:22Z
    - 0.6779562119291891 @ 2020-03-13T14:52:42Z
    - 0.5375642732426358 @ 2020-03-13T14:53:02Z
    - 0.651539593307087 @ 2020-03-13T14:53:22Z

  Maximum Values:
    - 0.9884358481450173 @ 2020-03-13T14:52:42Z
    - 0.5977883057658506 @ 2020-03-13T14:53:02Z
    - 0.7872863396943017 @ 2020-03-13T14:53:22Z

  Minimum Values:
    - 0.3663978219807138 @ 2020-03-13T14:52:42Z
    - 0.04379721593288575 @ 2020-03-13T14:53:02Z
    - 0.5375642732426358 @ 2020-03-13T14:53:22Z

  Percent Bad Values:
    - 25 % @ 2020-03-13T14:52:42Z
    - 50 % @ 2020-03-13T14:53:02Z
    - 0 % @ 2020-03-13T14:53:22Z

  Percent Good Values:
    - 75 % @ 2020-03-13T14:52:42Z
    - 50 % @ 2020-03-13T14:53:02Z
    - 100 % @ 2020-03-13T14:53:22Z

  Range Values:
    - 0.3663978219807138 @ 2020-03-13T14:52:42Z
    - 0.04379721593288575 @ 2020-03-13T14:53:02Z
    - 0.5375642732426358 @ 2020-03-13T14:53:22Z
```


## Next Steps

In the [final chapter](./06 - Adapter Configuration Options.md), we will modify our adapter to accept a set of configurable options, instead of hard-coding its settings.
