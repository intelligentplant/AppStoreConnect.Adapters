# Tag Snapshot Polling and Subscriptions

Snapshot polling and subscriptions for tags are implemented via the following features:

| Feature | Description |
| ------- | ----------- |
| [IReadSnapshotTagValues](../../src/DataCore.Adapter.Abstractions/RealTimeData/IReadSnapshotTagValues.cs) | Defines methods for polling the adapter for the current values of tags. |
| [ISnapshotTagValuePush](../../src/DataCore.Adapter.Abstractions/RealTimeData/ISnapshotTagValuePush.cs) | Allows a caller to subscribe to receive snapshot value changes for tags as they occur. |

The adapter toolkit defines a number of helper classes that can be used to simplify implementation of the above features depending on the adapter's requirements. The following table describes the appropriate helper class to use in different scenarios:

| Scenario | Example Source Systems | Helper Class |
| -------- | ------- | -------------- |
| Both real-time value change subscriptions and polling queries are supported by the source system. | SCADA system with OPC UA server | [SnapshotTagValuePush](#using-snapshottagvaluepush-to-manage-subscriptions-when-native-subscriptions-are-supported) |
| Polling for current values is supported by the source system, but value change subscriptions are not. | HTTP API, OpenTelemetry metric instrumentation | [PollingSnapshotTagValuePush](#using-pollingsnapshottagvaluepush-to-manage-subscriptions-when-only-polling-is-available) |
| An external message broker is used to push events to the adapter for processing. | MQTT broker, Azure Event Hub | [SnapshotTagValueManager](#using-snapshottagvaluemanager-to-manage-polling-and-subscriptions) |
| Current values are written to the adapter host by an external component, or are generated directly by the adapter | Incoming TCP stream | [SnapshotTagValueManager](#using-snapshottagvaluemanager-to-manage-polling-and-subscriptions) |


# Using SnapshotTagValuePush to manage subscriptions when native subscriptions are supported

The [SnapshotTagValuePush](../../src/DataCore.Adapter/RealTimeData/SnapshotTagValuePush.cs) class implements `ISnapshotTagValuePush`, managing subscriptions to the adapter on your behalf:

```cs
public class MyAdapter : AdapterBase<MyAdapterOptions> {

    private readonly SnapshotTagValuePush _snapshotPush;
    
    public MyAdapter(
        string id,
        IOptions<MyAdapterOptions> options,
        IBackgroundTaskService backgroundTaskService,
        ILogger<MyAdapter> logger
    ) : base(id, options, backgroundTaskService, logger) {
        // TODO: Implement the ITagInfo feature or delegate it to an external provider.

        _snapshotPush = new SnapshotTagValuePush(
            new SnapshotTagValuePushOptions() {
                TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this),
                OnTagSubscriptionsAdded = (sender, tags, cancellationToken) => {
                    // TODO: Perform any setup actions required when tags are subscribed to.
                },
                OnTagSubscriptionsRemoved = (sender, tags, cancellationToken) => {
                    // TODO: Perform any teardown actions required when tags are unsubscribed from.
                }
            },
            BackgroundTaskService,
            Logger
        );

        AddFeatures(_snapshotPush);
    }

}
```

The `SnapshotTagValuePush` constructor accepts a [SnapshotTagValuePushOptions](../../src/DataCore.Adapter/RealTimeData/SnapshotTagValuePushOptions.cs) parameter that can be used to define callbacks used by the `SnapshotTagValuePush` to handle events such as:

- Verifying that tag names or IDs specified when subscribing to tags are valid.
- Performing custom setup actions when the number of subscribers for a tag changes from zero to one.
- Performing custom teardown actions when the number of subscribers for a tag changes from one to zero.


## Resolving tag names and IDs

In virtually every case, your adapter will expose the [ITagInfo](../../src/DataCore.Adapter.Abstractions/Tags/ITagInfo.cs) feature (either by implementing it directly or by [using a helper class](./tag-search.md)). An appropriate callback for the `SnapshotTagValuePushOptions.TagResolver` property can easily be created from the `ITagInfo` feature by calling the static `SnapshotTagValuePush.CreateTagResolverFromAdapter(ITagInfo)` or `SnapshotTagValuePush.CreateTagResolverFromAdapter(IAdapter)` methods.


## Setup and teardown actions on subscription change

The setup and teardown actions in `SnapshotTagValuePushOptions` can be used to add or remove items to the underlying native push mechanism, for example by adding items to or removing items from a subscription on an OPC UA server.

Alternatively, instead of specifying callback methods in the `SnapshotTagValuePushOptions` object, you can create your own subclass of `SnapshotTagValuePush` and override the appropriate methods directly:

```cs
public class MySnapshotPush : SnapshotTagValuePush {

    public MySnapshotPush(SnapshotTagValuePushOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger)
        : base(options, backgroundTaskService, logger) { }

    protected override IAsyncEnumerable<TagIdentifier> ResolveTags(IAdapterCallContext context, IEnumerable<string> tags, [EnumeratorCancellation] CancellationToken cancellationToken) {
        // TODO: Return an IAsyncEnumerable<TagIdentifier> containing the resolved tags
    }

    protected override Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
        // TODO: Perform any setup actions required when tags are subscribed to.
    }

    protected override Task OnTagsRemoved(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
        // TODO: Perform any teardown actions required when tags are unsubscribed from.
    }

}
```


## Notifying the SnapshotTagValuePush about value changes

In order to be able to deliver value changes to subscribers, you must tell the `SnapshotTagValuePush` when the snapshot value for a tag has changed by calling its `ValueReceived` method:

```cs
private async Task OnValueChangedAsync(TagValueQueryResult newSnapshot, CancellationToken cancellationToken) {
    await _snapshotPush.ValueReceived(newSnapshot, cancellationToken).ConfigureAwait(false);
} 
```


# Using PollingSnapshotTagValuePush to manage subscriptions when only polling is available

The [PollingSnapshotTagValuePush](../../src/DataCore.Adapter/RealTimeData/PollingSnapshotTagValuePush.cs) class implements `ISnapshotTagValuePush`. Unlike the `SnapshotTagValuePush` class, it does not require you to tell it when snapshot values have changed. Instead, it accepts an `IReadSnapshotTagValues` parameter when it is created, and then periodically polls this provider for the current value for all tags that currently have subscribers. This ensures that polling only occurs when an external caller is actively observing value changes.

The constructor also accepts a [PollingSnapshotTagValuePushOptions](../../src/DataCore.Adapter/RealTimeData/PollingSnapshotTagValuePush.cs) parameter, which is used to configure how frequently the `IReadSnapshotTagValues` feature will be polled:

```cs
public class MyAdapter : AdapterBase<MyAdapterOptions> {

    private readonly PollingSnapshotTagValuePush _snapshotPush;
    
    public MyAdapter(
        string id,
        IOptions<MyAdapterOptions> options,
        IBackgroundTaskService backgroundTaskService,
        ILogger<MyAdapter> logger
    ) : base(id, options, backgroundTaskService, logger) {
        // TODO: Implement the ITagInfo and IReadSnapshotTagValues features or delegate them to external providers.

        _snapshotPush = new PollingSnapshotTagValuePush(
            this.GetFeature<IReadSnapshotTagValues>(),
            new PollingSnapshotTagValuePushOptions() {
                PollingInterval = TimeSpan.FromSeconds(15),
                TagResolver = PollingSnapshotTagValuePush.CreateTagResolverFromAdapter(this)
            },
            BackgroundTaskService,
            Logger
        );

        AddFeatures(_snapshotPush);
    }

}
```


## Resolving tag names and IDs

As with `SnapshotTagValuePush`, an appropriate callback for the `PollingSnapshotTagValuePushOptions.TagResolver` property can easily be created from the adapter's `ITagInfo` feature by calling the static `PollingSnapshotTagValuePush.CreateTagResolverFromAdapter(ITagInfo)` or `PollingSnapshotTagValuePush.CreateTagResolverFromAdapter(IAdapter)` methods, or by creating a subclass and overriding the `ResolveTags` method.


# Using SnapshotTagValueManager to manage polling and subscriptions

The [SnapshotTagValueManager](../../src/DataCore.Adapter/RealTimeData/SnapshotTagValueManager.cs) class implements both `IReadSnapshotTagValues` and `ISnapshotTagValuePush`. When a tag value change is written to it via the `ValueReceived` method, it caches the value in memory, optionally persists it to an [IKeyValueStore](../../src/DataCore.Adapter.Abstractions/Services/IKeyValueStore.cs) (if a suitable store was provided when creating the `SnapshotTagValueManager`), and publishes the value to any subscribers for the tag:

```cs
public class MyAdapter : AdapterBase<MyAdapterOptions> {

    private readonly SnapshotTagValueManager _snapshotManager;
    
    public MyAdapter(
        string id,
        IOptions<MyAdapterOptions> options,
        IBackgroundTaskService backgroundTaskService,
        IKeyValueStore keyValueStore,
        ILogger<MyAdapter> logger
    ) : base(id, options, backgroundTaskService, logger) {
        // TODO: Implement the ITagInfo feature or delegate it to an external provider.

        _snapshotManager = new SnapshotTagValueManager(
            new SnapshotTagValueManagerOptions() {
                TagResolver = SnapshotTagValueManager.CreateTagResolverFromAdapter(this)
            },
            BackgroundTaskService,
            keyValueStore,
            Logger
        );

        AddFeatures(_snapshotManager);
    }

    protected override async Task StartAsync(CancellationToken cancellationToken) {
        await _snapshotManager.InitAsync(cancellationToken).ConfigureAwait(false);
        // TODO: Other initialisation code.
    }

}
```

## Resolving tag names and IDs

As with `SnapshotTagValuePush` and `PollingSnapshotTagValuePush`, an appropriate callback for the `SnapshotTagValueManagerOptions.TagResolver` property can easily be created from the adapter's `ITagInfo` feature by calling the static `SnapshotTagValueManager.CreateTagResolverFromAdapter(ITagInfo)` or `SnapshotTagValueManager.CreateTagResolverFromAdapter(IAdapter)` methods, or by creating a subclass and overriding the `ResolveTags` method.


## Initialising the SnapshotTagValueManager

`SnapshotTagValueManager` must be initialised before it can be used, to allow it to load persisted tag values from the `IKeyValueStore`. `SnapshotTagValueManager` will lazily initialise itself the first time a method is invoked, but can be eagerly initialised by calling its `InitAsync` method. This should be performed in the adapter's `StartAsync` method:

```cs
protected override async Task StartAsync(CancellationToken cancellationToken) {
    await _snapshotManager.InitAsync(cancellationToken).ConfigureAwait(false);
    // TODO: Other initialisation code.
}
```

## Notifying the SnapshotTagValueManager about value changes

In order to be able to deliver value changes to subscribers, you must tell the `SnapshotTagValueManager` when the snapshot value for a tag has changed by calling its `ValueReceived` method:

```cs
private async Task OnValueChangedAsync(TagValueQueryResult newSnapshot, CancellationToken cancellationToken) {
    await _snapshotManager.ValueReceived(newSnapshot, cancellationToken).ConfigureAwait(false);
} 
```


## Example Use Cases

Use `SnapshotTagValueManager` when an external source writes data to the adapter that is then parsed and converted into tag values, or when the adapter itself generates new tag values at irregular intervals (for example, in response to external events occurring in the host application). You may also want to consider using a helper class [to manage the adapter's tag definitions](./tag-search.md) so that tag definitions can be dynamically created when required and discovered by consuming applications, as well as being persisted to the adapter's `IKeyValueStore` service between restarts: 

```cs
public class MyAdapter : AdapterBase<MyAdapterOptions> {

    private static readonly AdapterProperty s_tagCreatedAtPropertyDefinition = new AdapterProperty("UTC Created At", DateTime.MinValue, "The UTC creation time for the tag");

    private readonly ConfigurationChanges _configurationChanges;

    private readonly TagManager _tagManager;

    private readonly SnapshotTagValueManager _snapshotManager;
    
    public MyAdapter(
        string id,
        IOptions<MyAdapterOptions> options,
        IBackgroundTaskService backgroundTaskService,
        IKeyValueStore keyValueStore,
        ILogger<MyAdapter> logger
    ) : base(id, options, backgroundTaskService, logger) {
        _configurationChanges = new ConfigurationChanges(new ConfigurationChangesOptions() {
            Id = id
        }, BackgroundTaskService, Logger);

        AddFeatures(_configurationChanges);

        _tagManager = new TagManager(
            keyValueStore,
            BackgroundTaskService,
            new[] { s_tagCreatedAtPropertyDefinition },
            _configurationChanges.NotifyAsync
        );

        AddFeatures(_tagManager);

        _snapshotManager = new SnapshotTagValueManager(
            new SnapshotTagValueManagerOptions() {
                TagResolver = SnapshotTagValueManager.CreateTagResolverFromAdapter(this)
            },
            BackgroundTaskService,
            keyValueStore,
            Logger
        );

        AddFeatures(_snapshotManager);
    }

    protected override async Task StartAsync(CancellationToken cancellationToken) {
        await _snapshotManager.InitAsync(cancellationToken).ConfigureAwait(false);
        // TODO: Other initialisation code.
    }

    public async Task ProcessReceivedJsonMessageAsync(JsonElement json, CancellationToken cancellationToken) {
        foreach (var dataValue in ParseJsonMessage(json)) {
            var tag = await _tagManager.GetTagAsync(dataValue.TagId, cancellationToken).ConfigureAwait(false);

            if (tag == null) {
                tag = new TagDefinitionBuilder(dataValue.TagId, dataValue.TagName)
                    .WithDataType(dataValue.Value.Type)
                    .WithSupportsReadSnapshotValues()
                    .WithSupportsSnapshotValuePush()
                    .WithProperty(s_tagCreatedAtPropertyDefinition.Name, DateTime.UtcNow)
                    .Build();
                await _tagManager.AddOrUpdateTagAsync(tag, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private IEnumerable<TagValueQueryResult> ParseJsonMessage(JsonElement json) {
        // TODO: implement extraction of values from JSON message.
    }

}
```
