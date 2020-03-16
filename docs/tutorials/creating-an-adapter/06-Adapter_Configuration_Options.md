# Tutorial - Creating an Adapter

_This is part 6 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Adapter Configuration Options

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-06)._

An important feature when writing an adapter is the ability to provide configuration settings from an external source (e.g. a configuration file). Adapters will typically require some sort of connection settings, credentials, feature switches, and so on. We can easily modify our adapter to meet these requirements by making a few simple changes.

Up until now, we have used the `AdapterBase` as the base class for our adapter. In addition to this class, the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package also provides us with a second base class option, [AdapterBase<TOptions>](/src/DataCore.Adapter/AdapterBaseT.cs). `AdapterBase<TOptions>` extends `AdapterBase`, but uses a strongly-typed options class derived from [AdapterOptions](/src/DataCore.Adapter/AdapterOptions.cs) to provide configuration settings to the adapter.

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
        IBackgroundTaskService scheduler = null,
        ILogger<Adapter> logger = null
    ) : base(
        id, 
        options, 
        scheduler, 
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

Next, in `Program.cs`, we'll modify the adapter creation as follows:

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
    - Startup Time = 2020-03-16T10:07:05Z
  Features:
    - IHealthCheck
    - IReadTagValuesAtTimes
    - IReadProcessedTagValues
    - IReadSnapshotTagValues
    - IReadPlotTagValues
    - ITagSearch
    - ISnapshotTagValuePush
    - ITagInfo
    - IReadRawTagValues

  Supported Aggregations:
    - AVG
      - Name: Average
      - Description: Average value calculated over sample interval.
    - COUNT
      - Name: Count
      - Description: The number of good-quality raw samples that have been recorded for the tag at each sample interval.
    - INTERP
      - Name: Interpolated
      - Description: Interpolates a value at each sample interval based on the raw values on either side of the sample time for the interval.
    - MAX
      - Name: Maximum
      - Description: Maximum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the maximum value occurred at.
    - MIN
      - Name: Minimum
      - Description: Minimum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the minimum value occurred at.
    - PERCENTBAD
      - Name: Percent Bad
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have bad-quality status.
    - PERCENTGOOD
      - Name: Percent Good
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have good-quality status.
    - RANGE
      - Name: Range
      - Description: The difference between the minimum good-quality value and maximum good-quality value in each sample interval.
    - DELTA
      - Name: Delta
      - Description: The difference between the earliest good-quality value and latest good-quality value in each sample interval.

[Tag Details]
  Name: Sinusoid_Wave
  ID: 1
  Description: A tag that returns a sinusoid wave value
  Properties:
    - Wave Type = Sinusoid

  Raw Values (10:06:50.708 - 10:07:05.708 UTC):
    - 37.157241273870035 @ 2020-03-16T10:06:50.0000000Z [Good Quality]
    - 36.448431371071194 @ 2020-03-16T10:06:51.0000000Z [Good Quality]
    - 35.723633981637114 @ 2020-03-16T10:06:52.0000000Z [Good Quality]
    - 34.983167025665452 @ 2020-03-16T10:06:53.0000000Z [Good Quality]
    - 34.227355296431867 @ 2020-03-16T10:06:54.0000000Z [Good Quality]
    - 33.456530317940604 @ 2020-03-16T10:06:55.0000000Z [Good Quality]
    - 32.671030199503242 @ 2020-03-16T10:06:56.0000000Z [Good Quality]
    - 31.871199487432744 @ 2020-03-16T10:06:57.0000000Z [Good Quality]
    - 31.057389013914076 @ 2020-03-16T10:06:58.0000000Z [Good Quality]
    - 30.22995574311761 @ 2020-03-16T10:06:59.0000000Z [Good Quality]
    - 29.389262614622847 @ 2020-03-16T10:07:00.0000000Z [Bad Quality]
    - 28.535678384221107 @ 2020-03-16T10:07:01.0000000Z [Good Quality]
    - 27.669577462167073 @ 2020-03-16T10:07:02.0000000Z [Good Quality]
    - 26.791339748950051 @ 2020-03-16T10:07:03.0000000Z [Good Quality]
    - 25.901350468657093 @ 2020-03-16T10:07:04.0000000Z [Good Quality]
    - 24.999999999996032 @ 2020-03-16T10:07:05.0000000Z [Good Quality]
    - 24.087683705082117 @ 2020-03-16T10:07:06.0000000Z [Good Quality]

  Average Values (00:00:05 sample interval):
    - 34.967823598549245 @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 31.457393610991918 @ 2020-03-16T10:06:55.7081164Z [Uncertain Quality]
    - 26.779589212798271 @ 2020-03-16T10:07:00.7081164Z [Good Quality]

  Count Values (00:00:05 sample interval):
    - 5 @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 4 @ 2020-03-16T10:06:55.7081164Z [Uncertain Quality]
    - 5 @ 2020-03-16T10:07:00.7081164Z [Good Quality]

  Interpolated Values (00:00:05 sample interval):
    - 36.655321357215769 @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 32.900304801873169 @ 2020-03-16T10:06:55.7081164Z [Good Quality]
    - 28.784825622293997 @ 2020-03-16T10:07:00.7081164Z [Uncertain Quality]
    - 24.361738950989448 @ 2020-03-16T10:07:05.7081164Z [Good Quality]

  Maximum Values (00:00:05 sample interval):
    - 36.448431371071194 @ 2020-03-16T10:06:51.0000000Z [Good Quality]
    - 32.671030199503242 @ 2020-03-16T10:06:56.0000000Z [Uncertain Quality]
    - 28.535678384221107 @ 2020-03-16T10:07:01.0000000Z [Good Quality]

  Minimum Values (00:00:05 sample interval):
    - 33.456530317940604 @ 2020-03-16T10:06:55.0000000Z [Good Quality]
    - 30.22995574311761 @ 2020-03-16T10:06:59.0000000Z [Uncertain Quality]
    - 24.999999999996032 @ 2020-03-16T10:07:05.0000000Z [Good Quality]

  Percent Bad Values (00:00:05 sample interval):
    - 0 % @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 20 % @ 2020-03-16T10:06:55.7081164Z [Good Quality]
    - 0 % @ 2020-03-16T10:07:00.7081164Z [Good Quality]

  Percent Good Values (00:00:05 sample interval):
    - 100 % @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 80 % @ 2020-03-16T10:06:55.7081164Z [Good Quality]
    - 100 % @ 2020-03-16T10:07:00.7081164Z [Good Quality]

  Range Values (00:00:05 sample interval):
    - 2.9919010531305901 @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 2.4410744563856319 @ 2020-03-16T10:06:55.7081164Z [Uncertain Quality]
    - 3.5356783842250756 @ 2020-03-16T10:07:00.7081164Z [Good Quality]

  Delta Values (00:00:05 sample interval):
    - 2.9919010531305901 @ 2020-03-16T10:06:50.7081164Z [Good Quality]
    - 2.4410744563856319 @ 2020-03-16T10:06:55.7081164Z [Uncertain Quality]
    - 3.5356783842250756 @ 2020-03-16T10:07:00.7081164Z [Good Quality]
```


## Next Steps

This is the last part of this tutorial. It is recommended that you explore the remaining standard adapter features, and try adding more features to the example adapter that we have built!

If you want to host an adapter in an ASP.NET Core application, you can explore the projects in this repository for adding endpoints for querying adapters using [API controllers](/src/DataCore.Adapter.AspNetCore.Mvc), [SignalR](/src/DataCore.Adapter.AspNetCore.SignalR), and [gRPC](/src/DataCore.Adapter.AspNetCore.Grpc).
