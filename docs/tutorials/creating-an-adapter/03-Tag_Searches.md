# Tutorial - Creating an Adapter

_This is part 3 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Tag Searches

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-03)._

In the [previous chapter](./02-Reading_Current_Values.md), we implemented the `IReadSnapshotTagValues` interface. Our initial implementation returns a value for any tag specified by the caller. In a real-world implementation, we would ordinarily have a limited selection of tags to query. In this chapter, we will define a fixed set of tags that a caller can query, and we will use the [ITagSearch](/src/DataCore.Adapter.Abstractions/RealTimeData/ITagSearch.cs) interface to make these tags discoverable.

We will also update our `IReadSnapshotTagValues` implementation so that we only return values for known tags. We will also add some additional wave functions to our adapter, and allow each tag to specify which function it uses to calculate its values.

We will implement `ITagSearch` using the [TagManager](/src/DataCore.Adapter/Tags/TagManager.cs) helper class from the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package. This highlights an important aspect of adapter feature implementations: the adapter does not have to directly implement a feature interface itself. Instead, the implementation can be delegated to another class.

We can add the feature to our adapter by declaring a new field updating our constructor like so:

```csharp
private readonly TagManager _tagManager;

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
    _tagManager = new TagManager(
        backgroundTaskService: BackgroundTaskService,
        tagPropertyDefinitions: new[] { CreateWaveTypeProperty(null) }
    );

    AddFeatures(_tagManager);
}
```

Next, we will add some helper methods to create our tag definitions when the adapter starts up:

```csharp
private AdapterProperty CreateWaveTypeProperty(string waveType) {
    return AdapterProperty.Create("Wave Type", waveType ?? "Sinusoid", "The wave type for the tag");
}


private async Task CreateTagsAsync(CancellationToken cancellationToken) {
    var i = 0;
    foreach (var waveType in new[] { "Sinusoid", "Sawtooth", "Square", "Triangle" }) {
        ++i;
        var tagId = i.ToString();
        var tagName = string.Concat(waveType, "_Wave");

        var tag = new TagDefinitionBuilder(tagId, tagName)
            .WithDescription($"A tag that returns a {waveType.ToLower()} wave value")
            .WithDataType(VariantType.Double)
            .WithProperties(CreateWaveTypeProperty(waveType))
            .Build();

        await _tagManager.AddOrUpdateTagAsync(tag, cancellationToken).ConfigureAwait(false);
    }
}
```

Our `StartAsync` method is updated as follows:

```csharp
protected override async Task StartAsync(CancellationToken cancellationToken) {
    await CreateTagsAsync(cancellationToken).ConfigureAwait(false);
    AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
}
```

The `CreateWaveTypeProperty` method creates a property for our tag definitions that describes the type of the wave function used by the tag. The `CreateTagsAsync` method populates our `_tagManager` with 4 tag definitions.

Tags are defined as `TagDefinition` objects. A `TagDefinition` can hold a variety of information about a tag in addition to the ID and name, including: a description, engineering units, data type, discrete tag states (if required), custom properties, and labels/categories. We use the `TagDefinitionBuilder` class to simplify the construction of our `TagDefinition` instances.

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
public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
    IAdapterCallContext context,
    ReadSnapshotTagValuesRequest request,
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    ValidateInvocation(context, request);

    var sampleTime = CalculateSampleTime(DateTime.UtcNow);
    var rnd = new Random(sampleTime.GetHashCode());

    using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
        foreach (var tag in request.Tags) {
            if (ctSource.Token.IsCancellationRequested) {
                break;
            }
            if (string.IsNullOrWhiteSpace(tag)) {
                continue;
            }
            var tagDef = await _tagManager.GetTagAsync(tag, ctSource.Token).ConfigureAwait(false);
            if (tagDef == null) {
                continue;
            }

            yield return CalculateValueForTag(tagDef, sampleTime, rnd.NextDouble() < 0.9 ? TagValueStatus.Good : TagValueStatus.Bad);
        }
    }
}
```

As you can see, our implementation is almost identical to before, but now we try and retrieve the tag definition for each tag name or ID specified in the request, and skip the item if no matching tag definition is found. We also delegate the calculation of the snapshot value to the `CalculateValueForTag` method that we added above. The random number generator is used to determine if we assign `Good` or `Bad` quality to the value.


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

        var tagSearchFeature = adapter.GetFeature<ITagSearch>();
        var readSnapshotFeature = adapter.GetFeature<IReadSnapshotTagValues>();

        await foreach (var tag in tagSearchFeature.FindTags(
            context,
            new FindTagsRequest() {
                Name = "*"
            },
            cancellationToken
        )) {
            Console.WriteLine();
            Console.WriteLine("[Tag Details]");
            Console.WriteLine($"  Name: {tag.Name}");
            Console.WriteLine($"  ID: {tag.Id}");
            Console.WriteLine($"  Description: {tag.Description}");
            Console.WriteLine("  Properties:");
            foreach (var prop in tag.Properties) {
                Console.WriteLine($"    - {prop.Name} = {prop.Value}");
            }

            var value = await readSnapshotFeature.ReadSnapshotTagValues(
                context,
                new ReadSnapshotTagValuesRequest() {
                    Tags = new[] { tag.Id }
                },
                cancellationToken
            ).FirstOrDefaultAsync(cancellationToken);

            Console.WriteLine("  Snapshot Value:");
            Console.WriteLine($"    - {value.Value}");
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
    - Startup Time = 2020-09-18T09:59:25Z
  Features:
    - asc:features/real-time-data/tags/search/
    - asc:features/real-time-data/tags/info/
    - asc:features/real-time-data/values/read/snapshot/
    - asc:features/diagnostics/health-check/

[Tag Details]
  Name: Sawtooth_Wave
  ID: 2
  Description: A tag that returns a sawtooth wave value
  Properties:
    - Wave Type = Sawtooth
  Snapshot Value:
    - 0.16666666666680593 @ 2020-09-18T09:59:25.0000000Z [Bad Quality]

[Tag Details]
  Name: Sinusoid_Wave
  ID: 1
  Description: A tag that returns a sinusoid wave value
  Properties:
    - Wave Type = Sinusoid
  Snapshot Value:
    - 0.50000000000037892 @ 2020-09-18T09:59:25.0000000Z [Bad Quality]

[Tag Details]
  Name: Square_Wave
  ID: 3
  Description: A tag that returns a square wave value
  Properties:
    - Wave Type = Square
  Snapshot Value:
    - 1 @ 2020-09-18T09:59:25.0000000Z [Bad Quality]

[Tag Details]
  Name: Triangle_Wave
  ID: 4
  Description: A tag that returns a triangle wave value
  Properties:
    - Wave Type = Triangle
  Snapshot Value:
    - 0.33333333333361187 @ 2020-09-18T09:59:25.0000000Z [Bad Quality]
```

Note that the URIs for the `ITagSearch` and `ITagInfo` interfaces have been added to our adapter's feature set.


## Next Steps

In the [next chapter](04-Current_Value_Subscriptions.md), we will allow callers to create a subscription on our adapter and receive snapshot value changes as they occur.
