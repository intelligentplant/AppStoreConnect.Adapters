# Tutorial - Creating an Adapter

_This is part 6 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Adapter Configuration Options

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-06)._

An important feature when writing an adapter is the ability to provide configuration settings from an external source (e.g. a configuration file). Adapters will typically require some sort of connection settings, credentials, feature switches, and so on. We can easily modify our adapter to meet these requirements by making a few simple changes.

Up until now, we have used the `AdapterBase` as the base class for our adapter. In addition to this class, the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package also provides us with a second base class option, [AdapterBase&lt;TOptions&gt;](/src/DataCore.Adapter/AdapterBaseT.cs). `AdapterBase<TOptions>` extends `AdapterBase`, but uses a strongly-typed options class derived from [AdapterOptions](/src/DataCore.Adapter/AdapterOptions.cs) to provide configuration settings to the adapter.

Firstly, we will create our options class. Create a new file called `MyOptions.cs` and add the following code to it:

```csharp
using DataCore.Adapter;

namespace MyAdapter {
    public class MyAdapterOptions : AdapterOptions {

        public double Period { get; set; } = 60;

        public double Amplitude { get; set; } = 1;

    }
}
```

Our options class will allow the period and amplitude of the wave functions to be configurable, but will default to the values we have been using up until now. We inherit from the `AdapterOptions` base class, which provides properties for configuring the adapter's display name and description.

Next, we need to change the base class for our adapter from `AdapterBase` to `AdapterBase<MyAdapterOptions>`, and change our constructor signature to call one of the available `AdapterBase<TOptions>` constructors:

```csharp
public class Adapter : AdapterBase<MyAdapterOptions>, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues {
    // -- snip --

    public Adapter(
        string id,
        MyAdapterOptions options,
        IBackgroundTaskService backgroundTaskService = null,
        ILogger<Adapter> logger = null
    ) : base(
        id, 
        options, 
        backgroundTaskService, 
        logger
    ) {
        AddFeature<ISnapshotTagValuePush, PollingSnapshotTagValuePush>(PollingSnapshotTagValuePush.ForAdapter(
            this, 
            TimeSpan.FromSeconds(1)
        ));

        AddFeatures(ReadHistoricalTagValues.ForAdapter(this));
    }

    // -- snip --
}
```

As you can see, the only different here is that we have added in a `MyAdapterOptions` parameter that we pass to the base constructor, and removed the name and description parameters (since these are now supplied via the `MyAdapterOptions` class).

Our options are made available to us via the `Options` property defined in the base class. Our final change is to modify our `CalculateValueForTag` method so that it uses the period and amplitude specified in our options:

```csharp
private TagValueQueryResult CalculateValueForTag(
    TagDefinition tag,
    DateTime utcSampleTime,
    TagValueStatus status = TagValueStatus.Good
) {
    var waveType = tag.Properties.FindProperty("Wave Type")?.Value.GetValueOrDefault("Sinusoid");
    var period = Options.Period;
    var amplitude = Options.Amplitude;
    double value;

    switch (waveType) {
        case "Sawtooth":
            value = SawtoothWave(utcSampleTime, period, amplitude);
            break;
        case "Square":
            value = SquareWave(utcSampleTime, period, amplitude);
            break;
        case "Triangle":
            value = TriangleWave(utcSampleTime, period, amplitude);
            break;
        default:
            value = SinusoidWave(utcSampleTime, period, amplitude);
            break;
    }

    return new TagValueQueryResult(
        tag.Id,
        tag.Name,
        TagValueBuilder
            .Create()
            .WithUtcSampleTime(utcSampleTime)
            .WithValue(value)
            .WithStatus(status)
            .Build()
    );
}
```

Note that, since we are working with options specific to the adapter instance, the method is no longer `static`.

Next, in `Runner.cs`, we'll modify the adapter creation as follows:

```csharp
private static async Task Run(IAdapterCallContext context, CancellationToken cancellationToken) {
    await using (IAdapter adapter = new Adapter(AdapterId, new MyAdapterOptions() { 
        Name = AdapterDisplayName,
        Description = AdapterDescription,
        Period = 300,
        Amplitude = 50
    })) {
        await adapter.StartAsync(cancellationToken);

        // -- snip --

    }
}
```

When you run the program, you should see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-09-18T10:01:54Z
  Features:
    - asc:features/real-time-data/tags/info/
    - asc:features/real-time-data/values/read/at-times/
    - asc:features/real-time-data/values/read/processed/
    - asc:features/real-time-data/values/read/plot/
    - asc:features/real-time-data/values/read/snapshot/
    - asc:features/diagnostics/health-check/
    - asc:features/real-time-data/values/push/
    - asc:features/real-time-data/values/read/raw/
    - asc:features/real-time-data/tags/search/

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

  Raw Values (10:01:39.923 - 10:01:54.923 UTC):
    - 43.815334002193403 @ 2020-09-18T10:01:39.0000000Z [Good Quality]
    - 43.301270189222372 @ 2020-09-18T10:01:40.0000000Z [Bad Quality]
    - 42.768213008023061 @ 2020-09-18T10:01:41.0000000Z [Bad Quality]
    - 42.216396275098624 @ 2020-09-18T10:01:42.0000000Z [Good Quality]
    - 41.646062035503014 @ 2020-09-18T10:01:43.0000000Z [Good Quality]
    - 41.057460456683422 @ 2020-09-18T10:01:44.0000000Z [Good Quality]
    - 40.450849718745786 @ 2020-09-18T10:01:45.0000000Z [Bad Quality]
    - 39.826495901208439 @ 2020-09-18T10:01:46.0000000Z [Good Quality]
    - 39.184672866290832 @ 2020-09-18T10:01:47.0000000Z [Good Quality]
    - 38.52566213878854 @ 2020-09-18T10:01:48.0000000Z [Good Quality]
    - 37.849752782587153 @ 2020-09-18T10:01:49.0000000Z [Good Quality]
    - 37.15724127386931 @ 2020-09-18T10:01:50.0000000Z [Good Quality]
    - 36.448431371070455 @ 2020-09-18T10:01:51.0000000Z [Good Quality]
    - 35.72363398164034 @ 2020-09-18T10:01:52.0000000Z [Good Quality]
    - 34.983167025668742 @ 2020-09-18T10:01:53.0000000Z [Good Quality]
    - 34.227355296431078 @ 2020-09-18T10:01:54.0000000Z [Good Quality]
    - 33.456530317939801 @ 2020-09-18T10:01:55.0000000Z [Good Quality]

  Average Values (00:00:05 sample interval):
    - 41.639972922428349 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 38.846645922218741 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 35.707965789735987 @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Count Values (00:00:05 sample interval):
    - 3 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 4 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 5 @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Interpolated Values (00:00:05 sample interval):
    - 43.32304879094152 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 40.488971481815746 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 37.210115912582935 @ 2020-09-18T10:01:49.9236488Z [Good Quality]
    - 33.529251304344172 @ 2020-09-18T10:01:54.9236488Z [Uncertain Quality]

  Maximum Values (00:00:05 sample interval):
    - 42.216396275098624 @ 2020-09-18T10:01:42.0000000Z [Uncertain Quality]
    - 39.826495901208439 @ 2020-09-18T10:01:46.0000000Z [Uncertain Quality]
    - 37.15724127386931 @ 2020-09-18T10:01:50.0000000Z [Good Quality]

  Minimum Values (00:00:05 sample interval):
    - 41.057460456683422 @ 2020-09-18T10:01:44.0000000Z [Uncertain Quality]
    - 37.849752782587153 @ 2020-09-18T10:01:49.0000000Z [Uncertain Quality]
    - 34.227355296431078 @ 2020-09-18T10:01:54.0000000Z [Good Quality]

  Percent Bad Values (00:00:05 sample interval):
    - 40 % @ 2020-09-18T10:01:39.9236488Z [Good Quality]
    - 20 % @ 2020-09-18T10:01:44.9236488Z [Good Quality]
    - 0 % @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Percent Good Values (00:00:05 sample interval):
    - 60 % @ 2020-09-18T10:01:39.9236488Z [Good Quality]
    - 80 % @ 2020-09-18T10:01:44.9236488Z [Good Quality]
    - 100 % @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Range Values (00:00:05 sample interval):
    - 1.1589358184152019 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 1.976743118621286 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 2.9298859774382322 @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Delta Values (00:00:05 sample interval):
    - 1.1589358184152019 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 1.976743118621286 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 2.9298859774382322 @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Variance Values (00:00:05 sample interval):
    - 0.33581086577495545 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 0.72373157243817288 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 1.3416187198916085 @ 2020-09-18T10:01:49.9236488Z [Good Quality]

  Standard Deviation Values (00:00:05 sample interval):
    - 0.57949190311423282 @ 2020-09-18T10:01:39.9236488Z [Uncertain Quality]
    - 0.85072414591227685 @ 2020-09-18T10:01:44.9236488Z [Uncertain Quality]
    - 1.1582826597560756 @ 2020-09-18T10:01:49.9236488Z [Good Quality]
```


## Next Steps

This is the last part of this tutorial. It is recommended that you explore the remaining [standard adapter features](/src/DataCore.Adapter.Abstractions), and try adding more features to the example adapter that we have built!

If you want to host an adapter in an ASP.NET Core application, you can explore the projects in this repository for adding endpoints for querying adapters using [API controllers](/src/DataCore.Adapter.AspNetCore.Mvc), [SignalR](/src/DataCore.Adapter.AspNetCore.SignalR), and [gRPC](/src/DataCore.Adapter.AspNetCore.Grpc).
