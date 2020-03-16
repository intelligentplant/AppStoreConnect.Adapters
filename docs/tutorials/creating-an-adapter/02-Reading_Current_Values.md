# Tutorial - Creating an Adapter

_This is part 2 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Reading Current Values

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-02)._

In order to make our adapter useful, we need to start adding functionality to it. In this chapter, we will add the ability to read current values for instrument measurements to our adapter. Current values are also referred to as snapshot values. In this tutorial, we will use wave functions to compute numerical values for all measurements. In a real-world example, you might be returning readings from IoT sensors, instruments in an industrial process, Windows performance counters, and so on. An instrument measurement can also be referred to as a tag. 

Reading snapshot values is defined using the [IReadSnapshotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadSnapshotTagValues.cs) adapter feature. We'll extend our adapter class to implement this interface:

```csharp
public class Adapter : AdapterBase, IReadSnapshotTagValues {
    // -- snip --
}
```

Add the following methods to the adapter:

```csharp
private static DateTime CalculateSampleTime(DateTime queryTime) {
    var offset = queryTime.Ticks % TimeSpan.TicksPerSecond;
    return queryTime.Subtract(TimeSpan.FromTicks(offset));
}


private static double SinusoidWave(DateTime sampleTime, TimeSpan offset, double period, double amplitude) {
    var time = (sampleTime - DateTime.UtcNow.Date.Add(offset)).TotalSeconds;
    return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * time));
}


public ChannelReader<TagValueQueryResult> ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);
    var result = Channel.CreateUnbounded<TagValueQueryResult>();
    var now = DateTime.UtcNow;

    TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
        foreach (var tag in request.Tags) {
            if (ct.IsCancellationRequested) {
                break;
            }
            if (string.IsNullOrWhiteSpace(tag)) {
                continue;
            }

            ch.TryWrite(new TagValueQueryResult(
                tag,
                tag,
                TagValueBuilder
                    .Create()
                    .WithUtcSampleTime(now)
                    .WithValue(SinusoidWave(now, TimeSpan.Zero, 1, 1))
                    .Build()
            ));
        }
    }, result.Writer, true, cancellationToken);

    return result;
}
```

The `CalculateSampleTime` method will take a timestamp and round it down to the nearest second. Whenever someone requests the current value of a tag, they will receive the value at the start of the current second.

The `SinusoidWave` method will calculate a value for us using the provided sample time, period, and amplitude. The wave is assumed to start at midnight on the current UTC day, but can be offset using the `offset` parameter.

Let's walk through the `ReadSnapshotTagValues` method. First of all, we validate the request that is passed to the method:

```csharp
ValidateRequest(request);
```

The `ValidateRequest<TRequest>` method is inherited from `AdapterBase`. It will throw an exception if the request object is `null`, or if it fails validation using the [System.Componentmodel.DataAnnotations.Validator](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validator) class.

Next, we create our response channel:

```csharp
var result = Channel.CreateUnbounded<TagValueQueryResult>();
```

Adapter features make extensive use of [System.Threading.Channels](https://www.nuget.org/packages/System.Threading.Channels/) to publish query results to consumers.

After creating the channel, we get the current UTC time (which we will use when calculating values), and then we kick off a background operation that will publish the snapshot values to the result channel:

```csharp
TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
    foreach (var tag in request.Tags) {
        if (string.IsNullOrWhiteSpace(tag)) {
            continue;
        }

        ch.TryWrite(new TagValueQueryResult(
            tag,
            tag,
            TagValueBuilder
                .Create()
                .WithUtcSampleTime(now)
                .WithValue(SinusoidWave(now, TimeSpan.Zero, 60, 1)) // 60s period
                .Build()
        ));
    }
}, result.Writer, true, cancellationToken);
```

The `TaskScheduler` property is the `IBackgroundTaskService` that was passed into our constructor. A default implementation is provided if the constructor parameter was `null`. The `QueueBackgroundChannelOperation` extension method allows us to write values to our channel's writer (`result.Writer` in this case) in a background task. The `true` parameter indicates that the channel's writer should be completed once the background operation finishes.

The background operation itself is specified using a lambda function that accepts the channel writer, and a `CancellationToken` that will fire when either the `cancellationToken` parameter fires, or the adapter is disposed. In our lambda, we simply write a value into the channel for every tag specified in the request. The [TagValueBuilder](/src/DataCore.Adapter/RealTimeData/TagValueBuilder.cs) class simplifies the creation of tag values.

Note that we don't do any sort of checks on whether the tags specified in the request are valid (beyond ensuring that they are not `null` or white space); we will add this in later. Also, at this stage, all tags in the request will return exactly the same result.


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

        var readSnapshotFeature = adapter.GetFeature<IReadSnapshotTagValues>();
        var snapshotValues = readSnapshotFeature.ReadSnapshotTagValues(
            context,
            new ReadSnapshotTagValuesRequest() { 
                Tags = new[] { 
                    "Example 1",
                    "Example 2"
                }
            },
            cancellationToken
        );

        Console.WriteLine();
        Console.WriteLine("  Snapshot Values:");
        await foreach (var value in snapshotValues.ReadAllAsync(cancellationToken)) {
            Console.WriteLine($"    [{value.TagName}] - {value.Value}");
        }

    }
}
```

After displaying the initial adapter information, the `Run` method will now ask for the values of two tags, `Example 1` and `Example 2`, and will display the tag name, value and UTC sample time. When you run the program, you will see output similar to this:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-03-16T09:07:03Z
  Features:
    - IHealthCheck
    - IReadSnapshotTagValues

  Snapshot Values:
    [Example 1] - 0.30901699437453112 @ 2020-03-16T09:07:03.0000000Z [Good Quality]
    [Example 2] - 0.30901699437453112 @ 2020-03-16T09:07:03.0000000Z [Good Quality]
```

Note that the `IReadSnapshotTagValues` feature has automatically been detected and added to the list of available features.


## Next Steps

In the [next chapter](03-Tag_Searches.md), we will allow callers to search for available tags on our adapter and restrict data queries to only those tags.
