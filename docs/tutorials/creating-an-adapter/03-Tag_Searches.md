# Tutorial - Creating an Adapter

_This is part 3 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Tag Searches

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-03)._

In the [previous chapter](./02-Reading_Current_Values.md), we implemented the `IReadSnapshotTagValues` feature. Our initial implementation returns a value for any tag specified by the caller. In a real-world implementation, we would ordinarily have a limited selection of tags to query. In this chapter, we will define a fixed set of tags that a caller can query, and we will implement the [ITagSearch](/src/DataCore.Adapter.Abstractions/RealTimeData/ITagSearch.cs) feature to make these tags discoverable. We will also update our `IReadSnapshotTagValues` implementation so that we only return values for known tags. We will also add some additional wave functions to our adapter, and allow each tag to specify which function it uses to calculate its values.

First of all, we will extend our `Adapter` class to implement the `ITagSearch` interface:

```csharp
public class Adapter : AdapterBase, ITagSearch, IReadSnapshotTagValues {
    // -- snip --
}
```

The `ITagSearch` feature uses the [TagDefinition](/src/DataCore.Adapter.Core/RealTimeData/TagDefinition.cs) class to describe available tags. Tags can be identifier using both the tag name, and a unique tag identifier. The recommended behaviour for adapters is that tag names should be case-insensitive, but that identifiers should be case-sensitive. We will add two dictionaries to our adapter, to index tag definitions by both ID and name:

```csharp
private readonly ConcurrentDictionary<string, TagDefinition> _tagsById = new ConcurrentDictionary<string, TagDefinition>();

private readonly ConcurrentDictionary<string, TagDefinition> _tagsByName = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
```

Next, we will add some helper methods to create our tag definitions when the adapter starts up, and clean up when the adapter is shut down:

```csharp
private AdapterProperty CreateWaveTypeProperty(string waveType) {
    return AdapterProperty.Create("Wave Type", waveType ?? "Sinusoid", "The wave type for the tag");
}


private void CreateTags() {
    var i = 0;
    foreach (var waveType in new[] { "Sinusoid", "Sawtooth", "Square", "Triangle" }) {
        ++i;
        var tagId = i.ToString();
        var tagName = string.Concat(waveType, "_Wave");
        var tagProperties = new[] {
            CreateWaveTypeProperty(waveType)
        };

        var tag = new TagDefinition(
            tagId,
            tagName,
            $"A tag that returns a {waveType.ToLower()} wave value",
            null,
            VariantType.Double,
            null,
            tagProperties,
            null
        );

        _tagsById[tag.Id] = tag;
        _tagsByName[tag.Name] = tag;
    }
}


private void DeleteTags() {
    _tagsById.Clear();
    _tagsByName.Clear();
}
```

Our `StartAsync` and `StopAsync` methods are updated as follows:

```csharp
protected override Task StartAsync(CancellationToken cancellationToken) {
    CreateTags();
    AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    return Task.CompletedTask;
}


protected override Task StopAsync(CancellationToken cancellationToken) {
    DeleteTags();
    return Task.CompletedTask;
}
```

The `CreateWaveTypeProperty` method creates a property for our tag definitions that describes the type of the wave function used by the tag. The `CreateTags` method populates our `_tagsById` and `_tagsByName` maps with 4 tag definitions, and the `DeleteTags` method removes all entries from these two maps.

The `TagDefinition` can hold a variety of information about a tag in addition to the ID and name, including: a description, engineering units, data type, discrete tag states (if required), custom properties, and labels/categories. In our implementation above, we specify that our tags return `double` values.

Next, we must implement the `ITagSearch` feature. `ITagSearch` actually extends another feature, named [ITagInfo](/src/DataCore.Adapter.Abstractions/RealTimeData/ITagInfo.cs). `ITagInfo` allows callers to request information about tags if they know the ID or name of the tag, whereas `ITagSearch` allows search queries that match against a tag's name, description, and so on. The `GetTags` method (from `ITagInfo`) is implemented as follows:

```csharp
public Task<ChannelReader<TagDefinition>> GetTags(
    IAdapterCallContext context, 
    GetTagsRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);
    var result = Channel.CreateUnbounded<TagDefinition>();

    TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
        foreach (var tag in request.Tags) {
            if (ct.IsCancellationRequested) {
                break;
            }
            if (string.IsNullOrWhiteSpace(tag)) {
                continue;
            }

            if (_tagsById.TryGetValue(tag, out var t) || _tagsByName.TryGetValue(tag, out t)) {
                result.Writer.TryWrite(t);
            }
        }
    }, result.Writer, true, cancellationToken);
    
    return Task.FromResult(result.Reader);
}
```

Note that, again, we use the `TaskScheduler.QueueBackgroundChannelOperation` extension method to run a background operation that will publish tag definitions to our response channel. The background operation performs some simple validation on each tag in the request, and then returns the definition for the tag if it exists in either of our lookups.

Next, we implement the `GetTagProperties` and `FindTags` methods. The `GetTagProperties` method is used to provide callers with details of the properties that can be defined on our adapter's tag definitions:

```csharp
public Task<ChannelReader<AdapterProperty>> GetTagProperties(
    IAdapterCallContext context, 
    GetTagPropertiesRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);

    var result = new[] {
        CreateWaveTypeProperty(null),
    }.OrderBy(x => x.Name).SelectPage(request).PublishToChannel();

    return Task.FromResult(result);
}
```

The `GetTagPropertiesRequest` class implements the [IPageableAdapterRequest](/src/DataCore.Adapter.Core/Common/IPageableAdapterRequest.cs) interface, meaning that it specifies a page size and page number to apply to the tag properties. The `SelectPage` extension method allows us to apply the paging specified in an `IPageableAdapterRequest` to any `IOrderedEnumerable<T>`. The `PublishToChannel` extension method will take any `IEnumerable<T>` and return a `ChannelReader<T>` that will emit the contents of the enumerable.

`GetTagProperties` is important, because we can opt to allow callers to `FindTags` to include search filters that match against custom tag properties. In this case, `GetTagProperties` is the way that the available properties can be discovered.

Next, we implement the `FindTags` method:

```csharp
public Task<ChannelReader<TagDefinition>> FindTags(
    IAdapterCallContext context, 
    FindTagsRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);
    var result = Channel.CreateUnbounded<TagDefinition>();

    TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
        foreach (var tag in _tagsById.Values.ApplyFilter(request)) {
            if (ct.IsCancellationRequested) {
                break;
            }
            ch.TryWrite(tag);
        }
    }, result.Writer, true, cancellationToken);

    return Task.FromResult(result.Reader);
}
```

Since we are working with in-memory `TagDefinition` objects, we can take advantage of the `ApplyFilter` extension method in our tag search. `ApplyFilter` takes a `FindTagsRequest` object and applies the filters to our set of tag definitions. Filters can be exact matches, or they can include single-character or multi-character wildcards (`?` and `*` respectively). For example, it could contain a `Name` filter with a value of `"*_01"`, meaning that it would match against any tag name ending with `_01`. `ApplyFilter` will automatically sort matching tags by name and then apply the paging settings specified in the search filter.

We have configured our adapter to create tags with 4 different wave types (`Sinusoid`, `Sawtooth`, `Square`, and `Triangle`). We'll add some more helper methods to the adapter to calculate values for our additional wave types, and also a method (`CalculateValueForTag`) to select the correct calculation method for a tag based on the `Wave Type` property in the `TagDefinition`:

```csharp
private static double SinusoidWave(DateTime sampleTime, double period, double amplitude) {
    var time = (sampleTime - DateTime.UtcNow.Date).TotalSeconds;
    return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * time));
}


private static double SawtoothWave(DateTime sampleTime, double period, double amplitude) {
    var time = (sampleTime - DateTime.UtcNow.Date).TotalSeconds;
    return (2 * amplitude / Math.PI) * Math.Atan(1 / Math.Tan(Math.PI / period * time));
}


private static double SquareWave(DateTime sampleTime, double period, double amplitude) {
    return Math.Sign(SinusoidWave(sampleTime, period, amplitude));
}


private static double TriangleWave(DateTime sampleTime, double period, double amplitude) {
    var time = (sampleTime - DateTime.UtcNow.Date).TotalSeconds;
    return (2 * amplitude / Math.PI) * Math.Asin(Math.Sin(2 * Math.PI / period * time));
}


private static TagValueQueryResult CalculateValueForTag(
    TagDefinition tag, 
    DateTime utcSampleTime, 
    TagValueStatus status
) {
    var waveType = tag.Properties.FindProperty("Wave Type")?.Value.GetValueOrDefault("Sinusoid");
    double value;

    switch (waveType) {
        case "Sawtooth":
            value = SawtoothWave(utcSampleTime, 60, 1);
            break;
        case "Square":
            value = SquareWave(utcSampleTime, 60, 1);
            break;
        case "Triangle":
            value = TriangleWave(utcSampleTime, 60, 1);
            break;
        default:
            value = SinusoidWave(utcSampleTime, 60, 1);
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

Note that `CalculateValueForTag` also allows us to specify a _quality status_ for the value, using the [TagValueStatus](/src/DataCore.Adapter.Core/RealTimeData/TagValueStatus.cs) enum. The status allows your adapter to inform the caller if the value is trust-worthy. An instrument might report a non-good status for a value if it detected a fault in the instrument calibration for example.

Finally, we can update our `ReadSnapshotTagValues` method so that it will only return values for known tags:

```csharp
public Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);
    var result = Channel.CreateUnbounded<TagValueQueryResult>();
    var sampleTime = CalculateSampleTime(DateTime.UtcNow);

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

            var rnd = new Random(sampleTime.GetHashCode());
            ch.TryWrite(CalculateValueForTag(t, sampleTime, rnd.NextDouble() < 0.9 ? TagValueStatus.Good : TagValueStatus.Bad));
        }
    }, result.Writer, true, cancellationToken);

    return Task.FromResult(result.Reader);
}
```

As you can see, our implementation is almost identical to before, but now we try and retrieve the tag definition for each tag name or ID specified in the request, and skip the item if no matching tag definition is found. We also delegate the calculation of the snapshot value to the `CalculateValueForTag` method that we added above. The random number generator is used to determine if we assign `Good` or `Bad` quality to the value.


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
        var readSnapshotFeature = adapter.GetFeature<IReadSnapshotTagValues>();

        var tags = await tagSearchFeature.FindTags(
            context,
            new FindTagsRequest() { 
                Name = "*"
            },
            cancellationToken
        );
        
        await foreach(var tag in tags.ReadAllAsync(cancellationToken)) {
            Console.WriteLine();
            Console.WriteLine("[Tag Details]");
            Console.WriteLine($"  Name: {tag.Name}");
            Console.WriteLine($"  ID: {tag.Id}");
            Console.WriteLine($"  Description: {tag.Description}");
            Console.WriteLine("  Properties:");
            foreach (var prop in tag.Properties) {
                Console.WriteLine($"    - {prop.Name} = {prop.Value}");
            }

            var snapshotValues = await readSnapshotFeature.ReadSnapshotTagValues(
                context,
                new ReadSnapshotTagValuesRequest() { 
                    Tags = new[] { tag.Id }
                },
                cancellationToken
            );

            Console.WriteLine("  Snapshot Value:");
            await foreach (var value in snapshotValues.ReadAllAsync(cancellationToken)) {
                Console.WriteLine($"    - {value.Value}");
            }
        }
    }
}
```

Now, after displaying the initial adapter information, the `Run` method will search for a page of tags. It will then display information about each tag that is returned, and request the snapshot value for the tag. When you run the program, you should see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-03-16T09:10:29Z
  Features:
    - IHealthCheck
    - IReadSnapshotTagValues
    - ITagSearch
    - ITagInfo

[Tag Details]
  Name: Sawtooth_Wave
  ID: 2
  Description: A tag that returns a sawtooth wave value
  Properties:
    - Wave Type = Sawtooth
  Snapshot Value:
    - 0.033333333333406975 @ 2020-03-16T09:10:29.0000000Z [Good Quality]

[Tag Details]
  Name: Sinusoid_Wave
  ID: 1
  Description: A tag that returns a sinusoid wave value
  Properties:
    - Wave Type = Sinusoid
  Snapshot Value:
    - 0.10452846326788355 @ 2020-03-16T09:10:29.0000000Z [Good Quality]

[Tag Details]
  Name: Square_Wave
  ID: 3
  Description: A tag that returns a square wave value
  Properties:
    - Wave Type = Square
  Snapshot Value:
    - 1 @ 2020-03-16T09:10:29.0000000Z [Good Quality]

[Tag Details]
  Name: Triangle_Wave
  ID: 4
  Description: A tag that returns a triangle wave value
  Properties:
    - Wave Type = Triangle
  Snapshot Value:
    - 0.066666666666813951 @ 2020-03-16T09:10:29.0000000Z [Good Quality]
```

Note that the `ITagSearch` and `ITagInfo` features have been added to our adapter's feature set.


## Next Steps

In the [next chapter](04-Current_Value_Subscriptions.md), we will allow callers to create a subscription on our adapter and receive snapshot value changes as they occur.
