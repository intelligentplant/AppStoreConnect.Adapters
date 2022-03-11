# Tutorial - Creating an Adapter

_This is part 2 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Reading Current Values

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-02)._

In order to make our adapter useful, we need to start adding functionality to it. In this chapter, we will add the ability to read current values for instrument measurements to our adapter. Current values are also referred to as snapshot values. In this tutorial, we will use wave functions to compute numerical values for all measurements. In a real-world example, you might be returning readings from IoT sensors, instruments in an industrial process, Windows performance counters, and so on. An instrument measurement can also be referred to as a tag. 

Reading snapshot values is defined using the [IReadSnapshotTagValues](/src/DataCore.Adapter.Abstractions/RealTimeData/IReadSnapshotTagValues.cs) interface. We'll extend our adapter class to implement this interface:

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


public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    ValidateInvocation(context, request);

    await Task.Yield();

    var sampleTime = CalculateSampleTime(DateTime.UtcNow);

    using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
        foreach (var tag in request.Tags) {
            if (ctSource.Token.IsCancellationRequested) {
                break;
            }
            if (string.IsNullOrWhiteSpace(tag)) {
                continue;
            }

            yield return new TagValueQueryResult(
                tag,
                tag,
                new TagValueBuilder()
                    .WithUtcSampleTime(sampleTime)
                    .WithValue(SinusoidWave(sampleTime, TimeSpan.Zero, 60, 1))
                    .Build()
            );
        }
    }
}
```

The `CalculateSampleTime` method will take a timestamp and round it down to the nearest second. Whenever someone requests the current value of a tag, they will receive the value at the start of the current second.

The `SinusoidWave` method will calculate a value for us using the provided sample time, period, and amplitude. The wave is assumed to start at midnight on the current UTC day, but can be offset using the `offset` parameter.

Let's look at the `ReadSnapshotTagValues` method. Note that the return type for the method is `IAsyncEnumerable<T>`. Many adapter feature methods return this type, which allows for values to be streamed from an adapter as they are retrieved or computed, instead of using a waterfall approach that returns an entire data set on one go. By adding the `async` keyword to the declaration, we can make our method an async iterator (i.e. we can `yield return` results as they are calculated). Since we have declared the method to be `async`, we annotate the `CancellationToken` parameter with the `[EnumeratorCancellation]` attribute, otherwise we will get compiler warning `CS8424`.

Now, let's walk through the method implementation. First of all, we validate the `IAdapterCallContext` and request object that are passed to the method:

```csharp
ValidateInvocation(context, request);
```

The `ValidateInvocation` method is inherited from our base class. It will throw an exception if the context or request objects are `null`, or if the request object fails validation using the [System.Componentmodel.DataAnnotations.Validator](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validator) class.

Next, we `await` on `Task.CompletedTask`:

```csharp
await Task.Yield();
```

This step is only necessary to satisfy compiler warning `CS1998`, as the rest of our implementation is actually synchronous; if we were `await`-ing on another call inside our method, it would not be required.

Next, we get the current UTC time (which we will use when calculating values), and then we start our loop that will emit results back to the caller:

```csharp
using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
    foreach (var tag in request.Tags) {
        if (ctSource.Token.IsCancellationRequested) {
            break;
        }
        if (string.IsNullOrWhiteSpace(tag)) {
            continue;
        }

        yield return new TagValueQueryResult(
            tag,
            tag,
            new TagValueBuilder()
                .WithUtcSampleTime(sampleTime)
                .WithValue(SinusoidWave(sampleTime, TimeSpan.Zero, 60, 1))
                .Build()
        );
    }
}
```

We use the `CreateCancellationTokenSource` method inherited from our base class to create a `CancellationTokenSource` instance that will request cancellation when our `cancellationToken` parameter is cancelled, or if our adapter is stopped

The [TagValueBuilder](/src/DataCore.Adapter/RealTimeData/TagValueBuilder.cs) class simplifies the creation of tag values.

Note that we don't do any sort of checks on whether the tags specified in the request are valid (beyond ensuring that they are not `null` or white space); we will add this in later. Also, at this stage, all tags in the request will return exactly the same result.


## Testing

Modify the `Run` method in `Runner.cs` as follows:

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

        var readSnapshotFeature = adapter.GetFeature<IReadSnapshotTagValues>();

        Console.WriteLine();
        Console.WriteLine("  Snapshot Values:");
        await foreach (var value in readSnapshotFeature.ReadSnapshotTagValues(
            context,
            new ReadSnapshotTagValuesRequest() {
                Tags = new[] {
                    "Example 1",
                    "Example 2"
                }
            },
            cancellationToken
        )) {
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
    - Startup Time = 2020-09-18T09:58:52Z
  Features:
    - asc:features/real-time-data/values/read/snapshot/
    - asc:features/diagnostics/health-check/

  Snapshot Values:
    [Example 1] - -0.74314482547774752 @ 2020-09-18T09:58:52.0000000Z [Good Quality]
    [Example 2] - -0.74314482547774752 @ 2020-09-18T09:58:52.0000000Z [Good Quality]
```

Note that the URI for the `IReadSnapshotTagValues` interface (`asc:features/real-time-data/values/read/snapshot/`) has automatically been detected and added to the list of available features.


## Next Steps

In the [next chapter](03-Tag_Searches.md), we will allow callers to search for available tags on our adapter and restrict data queries to only those tags.
