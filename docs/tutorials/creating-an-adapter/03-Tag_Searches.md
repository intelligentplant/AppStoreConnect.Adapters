# Tutorial - Creating an Adapter

_This is part 3 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Tag Searches

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-03)._

In the [previous chapter](./02-Reading_Current_Values.md), we implemented the `IReadSnapshotTagValues` feature. Our initial implementation returns a value for any tag specified by the caller. In a real-world implementation, we would ordinarily have a limited selection of tags to query. In this chapter, we will define a fixed set of tags that a caller can query, and we will implement the [ITagSearch](/src/DataCore.Adapter.Abstractions/RealTimeData/ITagSearch.cs) feature to make these tags discoverable. We will also update our `IReadSnapshotTagValues` implementation so that we only return values for known tags.

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
private AdapterProperty CreateMinimumValueProperty(double min) {
    return new AdapterProperty("MinValue", min, "The inclusive minimum value for the tag");
}


private AdapterProperty CreateMaximumValueProperty(double max) {
    return new AdapterProperty("MaxValue", max, "The exclusive maximum value for the tag");
}


private void CreateTags() {
    for (var i = 0; i < 5; i++) {
        var tagId = (i + 1).ToString();
        var tagName = string.Concat("RandomValue_", tagId);
        // Our tags can have a minimum value of 0 and a maximum value of 1. We'll add 
        // properties to the tag to describe this.
        var tagProperties = new[] { 
            CreateMinimumValueProperty(0),
            CreateMaximumValueProperty(1)
        };

        var tag = new TagDefinition(
            tagId,
            tagName,
            "A tag that returns a random value",
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

The `CreateMinimumValueProperty` and `CreateMaximumValueProperty` methods create properties for our tag definitions that describe the minimum and maximum tag values respectively. The `CreateTags` method populates our `_tagsById` and `_tagsByName` maps with 5 tag definitions, and the `DeleteTags` method removes all entries from these two maps.

The `TagDefinition` can hold a variety of information about a tag in addition to the ID and name, including: a description, engineering units, data type, discrete tag states (if required), custom properties, and labels/categories. In our implementation above, we specify that our tags return `double` values, with an inclusive minimum value of zero and an exclusive maximum value of one.

Next, we must implement the `ITagSearch` feature. `ITagSearch` actually extends another feature, named [ITagInfo](/src/DataCore.Adapter.Abstractions/RealTimeData/ITagInfo.cs). `ITagInfo` allows callers to request information about tags if they know the ID or name of the tag, whereas `ITagSearch` allows search queries that match against a tag's name, description, and so on. The `GetTags` method (from `ITagInfo`) is implemented as follows:

```csharp
public ChannelReader<TagDefinition> GetTags(
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
    
    return result;
}
```

Note that, again, we use the `TaskScheduler.QueueBackgroundChannelOperation` extension method to run a background operation that will publish tag definitions to our response channel. The background operation performs some simple validation on each tag in the request, and then returns the definition for the tag if it exists in either of our lookups.

Next, we implement the `GetTagProperties` and `FindTags` methods. The `GetTagProperties` method is used to provide callers with details of the properties that can be defined on our adapter's tag definitions:

```csharp
public ChannelReader<AdapterProperty> GetTagProperties(
    IAdapterCallContext context, 
    GetTagPropertiesRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);

    return new[] {
        CreateMinimumValueProperty(0),
        CreateMaximumValueProperty(1)
    }.OrderBy(x => x.Name).SelectPage(request).PublishToChannel();
}
```

The `GetTagPropertiesRequest` implements the [IPageableAdapterRequest](/src/DataCore.Adapter.Core/Common/IPageableAdapterRequest.cs) interface, meaning that it specifies a page size and page number to apply to the tag properties. The `SelectPage` extension method allows us to apply the paging specified in an `IPageableAdapterRequest` to any `IOrderedEnumerable<T>`. The `PublishToChannel` extension method will take any `IEnumerable<T>` and return a `ChannelReader<T>` that will emit the contents of the enumerable.

`GetTagProperties` is important, because we can opt to allow callers to `FindTags` to include search filters that match against custom tag properties. In this case, `GetTagProperties` is the way that the available properties can be discovered.

Next, we implement the `FindTags` method:

```csharp
public ChannelReader<TagDefinition> FindTags(
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

    return result;
}
```

Since we are working with in-memory `TagDefinition` objects, we can take advantage of the `ApplyFilter` extension method in our tag search. `ApplyFilter` takes a `FindTagsRequest` object and applies the filters to our set of tag definitions. Filters can be exact matches, or they can include single-character or multi-character wildcards (`?` and `*` respectively). For example, it could contain a `Name` filter with a value of `"*_01"`, meaning that it would match against any tag name ending with `_01`. `ApplyFilter` will automatically sort matching tags by name and then apply the paging settings specified in the search filter.

Finally, we can update our `ReadSnapshotTagValues` method so that it will only return values for known tags:

```csharp
public ChannelReader<TagValueQueryResult> ReadSnapshotTagValues(
    IAdapterCallContext context, 
    ReadSnapshotTagValuesRequest request, 
    CancellationToken cancellationToken
) {
    ValidateRequest(request);
    var result = Channel.CreateUnbounded<TagValueQueryResult>();

    var rnd = new Random();

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

            ch.TryWrite(new TagValueQueryResult(
                t.Id,
                t.Name,
                TagValueBuilder
                    .Create()
                    .WithValue(rnd.NextDouble())
                    .Build()
            ));
        }
    }, result.Writer, true, cancellationToken);

    return result;
}
```

As you can see, our implementation is almost identical to before, but now we try and retrieve the tag definition for each tag name or ID specified in the request, and skip the item if no matching tag definition is found.


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

        var tags = tagSearchFeature.FindTags(
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

            var snapshotValues = readSnapshotFeature.ReadSnapshotTagValues(
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
    - Startup Time = 2020-03-15T15:49:02Z
  Features:
    - IHealthCheck
    - IReadSnapshotTagValues
    - ITagSearch
    - ITagInfo

[Tag Details]
  Name: RandomValue_1
  ID: 1
  Description: A tag that returns a random value
  Properties:
    - MinValue = 0
    - MaxValue = 1
  Snapshot Value:
    - 0.29634930393488579 @ 2020-03-15T15:49:02.8638725Z [Good Quality]

[Tag Details]
  Name: RandomValue_2
  ID: 2
  Description: A tag that returns a random value
  Properties:
    - MinValue = 0
    - MaxValue = 1
  Snapshot Value:
    - 0.31871352732121644 @ 2020-03-15T15:49:02.8783799Z [Good Quality]

[Tag Details]
  Name: RandomValue_3
  ID: 3
  Description: A tag that returns a random value
  Properties:
    - MinValue = 0
    - MaxValue = 1
  Snapshot Value:
    - 0.70797695950976436 @ 2020-03-15T15:49:02.9000434Z [Good Quality]

[Tag Details]
  Name: RandomValue_4
  ID: 4
  Description: A tag that returns a random value
  Properties:
    - MinValue = 0
    - MaxValue = 1
  Snapshot Value:
    - 0.543595817658862 @ 2020-03-15T15:49:02.9048014Z [Good Quality]

[Tag Details]
  Name: RandomValue_5
  ID: 5
  Description: A tag that returns a random value
  Properties:
    - MinValue = 0
    - MaxValue = 1
  Snapshot Value:
    - 0.8137845079478736 @ 2020-03-15T15:49:02.9101078Z [Good Quality]
```

Note that the `ITagSearch` and `ITagInfo` features have been added to our adapter's feature set.


## Next Steps

In the [next chapter](04-Current_Value_Subscriptions.md), we will allow callers to create a subscription on our adapter and receive snapshot value changes as they occur.
