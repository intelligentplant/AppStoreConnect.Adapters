# Tutorial - Creating an Adapter

_This is part 4 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


# Current Value Subscriptions

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-04)._

Adapters can interface with tens of thousands of different measurements, which may be changing value very rapidly. It can often be inefficient to poll large numbers of tags for snapshot values. Instead of polling, it is far more efficient to create a persistent connection to the adapter, choose tags that we want to subscribe to, and have the adapter push those values back to us as they change. 

Snapshot tag value subscriptions are implemented using the [ISnapshotTagValuePush](/src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) interface. In this chapter, we will implement this feature in our adapter, using one of the helper classes available to us in the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package. This highlights an important aspect of adapter feature implementations: the adapter does not have to directly implement a feature interface itself. Instead, the implementation can be delegated to another class.

Two helper classes can be used when implementing `ISnapshotTagValuePush`. The first, [SnapshotTagValuePush](/src/DataCore.Adapter/RealTimeData/SnapshotTagValuePush.cs), is used when the system you are connecting to defines its own native subscription system. The `SnapshotTagValuePush` constructor accepts an options parameter where you can define callbacks to be invoked when a subscription is added or removed for a given tag. Alternatively, you can extend `SnapshotTagValuePush` and override its `ResolveTags`, `OnTagsAddedToSubscription`, and `OnTagsRemovedFromSubscription` methods to perform the bootstrapping required to interface with the native subscription mechanism. You can then call the `ValueReceived` method to send a value for a tag to all subscribers to that tag. 

The second helper class is [PollingSnapshotTagValuePush](/src/DataCore.Adapter/RealTimeData/PollingSnapshotTagValuePush.cs). This class extends `SnapshotTagValuePush` by periodically polling the snapshot value of all tags that all callers have subscribed to and pushing the new values to the subscribed callers. `PollingSnapshotTagValuePush` is only compatible with adapters that implement `ITagInfo` (in order to resolve tag IDs or names that callers subscribe to) and `IReadSnapshotTagValues` (in order to poll for new values). Since we are just generating random values in our adapter, and we have already implemented both of the required features, this second helper class is a good fit for us.

We can add the feature to our adapter by updating our constructor like so:

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
    AddFeatures(new PollingSnapshotTagValuePush(this, new PollingSnapshotTagValuePushOptions() {
        AdapterId = id,
        PollingInterval = TimeSpan.FromSeconds(1),
        TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
    }, BackgroundTaskService, Logger));
}
```

Note that we don't add `ISnapshotTagValuePush` to the interface implementations for our adapter class. Instead, we use the `AddFeatures` method to register the `PollingSnapshotTagValuePush` object that we are delegating this feature to. We use the static `SnapshotTagValuePush.CreateTagResolverFromAdapter` method to define a delegate that can resolve a tag ID or name to a tag definition, and specify that we will update the current value of any subscribed tag every second.

That's it! We've done everything we need to do in order to enable snapshot tag value subscriptions on our adapter.


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
        var snapshotPushFeature = adapter.GetFeature<ISnapshotTagValuePush>();

        var tag = await tagSearchFeature.FindTags(
            context,
            new FindTagsRequest() {
                Name = "Sin*",
                PageSize = 1
            },
            cancellationToken
        ).FirstOrDefaultAsync(cancellationToken);

        Console.WriteLine();
        Console.WriteLine("[Tag Details]");
        Console.WriteLine($"  Name: {tag.Name}");
        Console.WriteLine($"  ID: {tag.Id}");
        Console.WriteLine($"  Description: {tag.Description}");
        Console.WriteLine("  Properties:");
        foreach (var prop in tag.Properties) {
            Console.WriteLine($"    - {prop.Name} = {prop.Value}");
        }

        try {
            Console.WriteLine("  Snapshot Value:");
            await foreach (var value in snapshotPushFeature.Subscribe(context, new CreateSnapshotTagValueSubscriptionRequest() {
                Tags = new[] { tag.Id },
                PublishInterval = TimeSpan.FromSeconds(1)
            }, cancellationToken)) {
                Console.WriteLine($"    - {value.Value}");
            }
        }
        catch (OperationCanceledException) { }
    }
}
```

After displaying the usual adapter information, the `Run` method searches for a single tag with a name starting with `Sin` (i.e. our `Sinusoid_Wave` tag), and then creates a subscription on that tag and uses an `await foreach` loop to iterate over every value returned by the `IAsyncEnumerable<T>` returned by the `Subscribe` method. The `await foreach` loop will continue until the program is stopped (via `CTRL+C`).

Run the program and wait until it receives a few value updates, and then press `CTRL+C` to cancel the subscription and exit. You should see output similar to the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-09-18T10:00:07Z
  Features:
    - asc:features/real-time-data/values/read/snapshot/
    - asc:features/real-time-data/tags/search/
    - asc:features/real-time-data/values/push/
    - asc:features/real-time-data/tags/info/
    - asc:features/diagnostics/health-check/

[Tag Details]
  Name: Sinusoid_Wave
  ID: 1
  Description: A tag that returns a sinusoid wave value
  Properties:
    - Wave Type = Sinusoid
  Snapshot Value:
    - 0.74314482547703276 @ 2020-09-18T10:00:08.0000000Z [Good Quality]
    - 0.809016994374672 @ 2020-09-18T10:00:09.0000000Z [Good Quality]
    - 0.8660254037842402 @ 2020-09-18T10:00:10.0000000Z [Good Quality]
    - 0.91354545764246864 @ 2020-09-18T10:00:11.0000000Z [Good Quality]
```

Note that the URI for `ISnapshotTagValuePush` is included in the adapter's features, even though we did not explicitly implement this interface on our adapter class!


## Next Steps

In the [next chapter](05-Historical_Value_Queries.md), we will implement historical tag value queries, and allow callers to requested aggregated data with the use of another helper class.
