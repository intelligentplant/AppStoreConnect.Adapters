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
    - Startup Time = 2020-03-16T10:45:21Z
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

  Raw Values (10:45:06.938 - 10:45:21.938 UTC):
    - 6.2666616782157236 @ 2020-03-16T10:45:06.0000000Z [Bad Quality]
    - 7.3041514281206759 @ 2020-03-16T10:45:07.0000000Z [Good Quality]
    - 8.3384373358047945 @ 2020-03-16T10:45:08.0000000Z [Good Quality]
    - 9.3690657292855004 @ 2020-03-16T10:45:09.0000000Z [Good Quality]
    - 10.395584540892386 @ 2020-03-16T10:45:10.0000000Z [Good Quality]
    - 11.417543505536777 @ 2020-03-16T10:45:11.0000000Z [Good Quality]
    - 12.434494358246303 @ 2020-03-16T10:45:12.0000000Z [Good Quality]
    - 13.445991030766422 @ 2020-03-16T10:45:13.0000000Z [Good Quality]
    - 14.451589847226295 @ 2020-03-16T10:45:14.0000000Z [Good Quality]
    - 15.450849718749671 @ 2020-03-16T10:45:15.0000000Z [Bad Quality]
    - 16.443332336931046 @ 2020-03-16T10:45:16.0000000Z [Good Quality]
    - 17.428602366092239 @ 2020-03-16T10:45:17.0000000Z [Good Quality]
    - 18.406227634234973 @ 2020-03-16T10:45:18.0000000Z [Good Quality]
    - 19.375779322605826 @ 2020-03-16T10:45:19.0000000Z [Good Quality]
    - 20.336832153790297 @ 2020-03-16T10:45:20.0000000Z [Good Quality]
    - 21.288964578253537 @ 2020-03-16T10:45:21.0000000Z [Good Quality]
    - 22.231758959245905 @ 2020-03-16T10:45:22.0000000Z [Good Quality]

  Average Values (00:00:05 sample interval):
    - 9.3649565079280261 @ 2020-03-16T10:45:06.9386690Z [Good Quality]
    - 14.193851893292518 @ 2020-03-16T10:45:11.9386690Z [Uncertain Quality]
    - 19.367281210995376 @ 2020-03-16T10:45:16.9386690Z [Good Quality]

  Count Values (00:00:05 sample interval):
    - 5 @ 2020-03-16T10:45:06.9386690Z [Good Quality]
    - 4 @ 2020-03-16T10:45:11.9386690Z [Uncertain Quality]
    - 5 @ 2020-03-16T10:45:16.9386690Z [Good Quality]

  Interpolated Values (00:00:05 sample interval):
    - 7.2405211442692554 @ 2020-03-16T10:45:06.9386690Z [Uncertain Quality]
    - 12.372123745498776 @ 2020-03-16T10:45:11.9386690Z [Good Quality]
    - 17.368174769933752 @ 2020-03-16T10:45:16.9386690Z [Good Quality]
    - 22.182701768992022 @ 2020-03-16T10:45:21.9386690Z [Good Quality]

  Maximum Values (00:00:05 sample interval):
    - 11.417543505536777 @ 2020-03-16T10:45:11.0000000Z [Good Quality]
    - 16.443332336931046 @ 2020-03-16T10:45:16.0000000Z [Uncertain Quality]
    - 21.288964578253537 @ 2020-03-16T10:45:21.0000000Z [Good Quality]

  Minimum Values (00:00:05 sample interval):
    - 7.3041514281206759 @ 2020-03-16T10:45:07.0000000Z [Good Quality]
    - 12.434494358246303 @ 2020-03-16T10:45:12.0000000Z [Uncertain Quality]
    - 17.428602366092239 @ 2020-03-16T10:45:17.0000000Z [Good Quality]

  Percent Bad Values (00:00:05 sample interval):
    - 0 % @ 2020-03-16T10:45:06.9386690Z [Good Quality]
    - 20 % @ 2020-03-16T10:45:11.9386690Z [Good Quality]
    - 0 % @ 2020-03-16T10:45:16.9386690Z [Good Quality]

  Percent Good Values (00:00:05 sample interval):
    - 100 % @ 2020-03-16T10:45:06.9386690Z [Good Quality]
    - 80 % @ 2020-03-16T10:45:11.9386690Z [Good Quality]
    - 100 % @ 2020-03-16T10:45:16.9386690Z [Good Quality]

  Range Values (00:00:05 sample interval):
    - 4.1133920774161012 @ 2020-03-16T10:45:06.9386690Z [Good Quality]
    - 4.0088379786847437 @ 2020-03-16T10:45:11.9386690Z [Uncertain Quality]
    - 3.8603622121612986 @ 2020-03-16T10:45:16.9386690Z [Good Quality]

  Delta Values (00:00:05 sample interval):
    - 4.1133920774161012 @ 2020-03-16T10:45:06.9386690Z [Good Quality]
    - 4.0088379786847437 @ 2020-03-16T10:45:11.9386690Z [Uncertain Quality]
    - 3.8603622121612986 @ 2020-03-16T10:45:16.9386690Z [Good Quality]
```


## Next Steps

This is the last part of this tutorial. It is recommended that you explore the remaining [standard adapter features](/src/DataCore.Adapter.Abstractions), and try adding more features to the example adapter that we have built!

If you want to host an adapter in an ASP.NET Core application, you can explore the projects in this repository for adding endpoints for querying adapters using [API controllers](/src/DataCore.Adapter.AspNetCore.Mvc), [SignalR](/src/DataCore.Adapter.AspNetCore.SignalR), and [gRPC](/src/DataCore.Adapter.AspNetCore.Grpc).
