# Tag Discovery and Search

Tag discovery and search is implemented via the following features:

| Feature | Description |
| ------- | ----------- |
| [ITagInfo](../../src/DataCore.Adapter.Abstractions/Tags/ITagInfo.cs) | Defines methods for retrieving details about known tags by name or ID, and about the adapter-specific properties that tags can define. | 
| [ITagSearch](../../src/DataCore.Adapter.Abstractions/Tags/ITagSearch.cs) | Defines methods for searching for tags using filters on name, description and so on. |

Note that `ITagSearch` is derived from `ITagInfo`.

# Managing tags using the TagManager class

If you do not have an external resource that defines the available tags (for example, if your adapter will build a tag list dynamically based on input it receives from an external system such as an MQTT broker), you can use the [TagManager](../../src/DataCore.Adapter/Tags/TagManager.cs) class to implement `ITagSearch` and `ITagInfo` on your behalf:

```cs
public class MyAdapter : AdapterBase<MyAdapterOptions> {

    private static readonly AdapterProperty s_tagCreatedAtPropertyDefinition = new AdapterProperty("UTC Created At", DateTime.MinValue, "The UTC creation time for the tag");

    private readonly ConfigurationChanges _configurationChanges;

    private readonly TagManager _tagManager;
    

    public MyAdapter(
        string id,
        IOptions<MyAdapterOptions> options,
        IBackgroundTaskService backgroundTaskService,
        ILoggerFactory loggerFactory
    ) : base(id, options, backgroundTaskService, loggerFactory) {

        _configurationChanges = new ConfigurationChanges(new ConfigurationChangesOptions() {
            Id = id
        }, BackgroundTaskService, LoggerFactory.CreateLogger<ConfigurationChanges>());

        AddFeatures(_configurationChanges);

        _tagManager = new TagManager(
            null,
            BackgroundTaskService,
            new[] { s_tagCreatedAtPropertyDefinition },
            _configurationChanges.NotifyAsync,
            LoggerFactory.CreateLogger<TagManager>()
        );

        AddFeatures(_tagManager);
    }

}
```

In the above example, we are also using the [ConfigurationChanges](../../src/DataCore.Adapter/Diagnostics/ConfigurationChanges.cs) helper class to implement the [IConfigurationChanges](../../src/DataCore.Adapter.Abstractions/Diagnostics/IConfigurationChanges.cs) feature on our behalf. This allows external subscribers to be notified when tags are created, updated or deleted from the `TagManager`.

We define an `AdapterProperty` describing a property that will be present on all of the tags that we create (the UTC time that the tag was created or last updated at) and inform the `TagManager` about this property when we create it.

We register the `TagManager` with the adapter by calling the `AddFeatures` method. Note that, even though `TagManager` implements `IDisposable`, we do not need to manage its lifetime ourselves. This is because `AdapterBase<TAdapterOptions>` automatically disposes of any registered features that implement `IDisposable` or `IAsyncDisposable` when the adapter is disposed.

The `TagManager` also allows us to persist tag definitions to an [IKeyValueStore](../../src/DataCore.Adapter.Abstractions/Services/IKeyValueStore.cs) if desired. In the example above, we have specified `null` for this parameter, meaning that definitions will be stored in-memory but will not be persisted between restarts of the adapter. If persistence of tag definitions is required, we could modify our constructor to accept an `IKeyValueStore` parameter and pass this into the `TagManager` constructor:

```cs
public MyAdapter(
    string id,
    IOptions<MyAdapterOptions> options,
    IBackgroundTaskService backgroundTaskService,
    IKeyValueStore keyValueStore,
    ILoggerFactory loggerFactory
) : base(id, options, backgroundTaskService, loggerFactory) {

    _configurationChanges = new ConfigurationChanges(new ConfigurationChangesOptions() {
        Id = id
    }, BackgroundTaskService, LoggerFactory.CreateLogger<ConfigurationChanges>());

    AddFeatures(_configurationChanges);

    _tagManager = new TagManager(
        keyValueStore,
        BackgroundTaskService,
        new[] { s_tagCreatedAtPropertyDefinition },
        _configurationChanges.NotifyAsync,
        LoggerFactory.CreateLogger<TagManager>()
    );

    AddFeatures(_tagManager);
}
```

## Initialising the TagManager

`TagManager` must be initialised before it can be used, to allow it to load tag definitions from the `IKeyValueStore`. `TagManager` will lazily initialise itself the first time a method is invoked, but can be eagerly initialised by calling its `InitAsync` method. This should be performed in the adapter's `StartAsync` method:

```cs
protected override async Task StartAsync(CancellationToken cancellationToken) {
    await _tagManager.InitAsync(cancellationToken).ConfigureAwait(false);
    // TODO: Other initialisation code.
}
```


## Creating a tag definition

To create a tag definition (or to update an existing definition with the same ID), call the `AddOrUpdateTagAsync` method on the `TagManager`. The [TagDefinitionBuilder](../../src/DataCore.Adapter/Tags/TagDefinitionBuilder.cs) class can be used to create the actual tag definition:

```cs
private async Task CreateOrUpdateTagAsync(string id, string name, string? description = null, string? units = null, CancellationToken cancellationToken = default) {
    var tag = await _tagManager.GetTagAsync(id, cancellationToken).ConfigureAwait(false);

    if (tag == null) {
        tag = new TagDefinitionBuilder(id, name)
            .WithDescription(description)
            .WithUnits(units)
            .WithProperty(s_tagCreatedAtPropertyDefinition.Name, DateTime.UtcNow)
            .Build();
        await _tagManager.AddOrUpdateTagAsync(tag, cancellationToken).ConfigureAwait(false);
    }
}
```

Passing a `TagDefinition` to `AddOrUpdateTagAsync` that has an ID that already exists will cause the `TagManager` to replace the existing definition with the new one.


## Deleting a tag definition

To delete a tag definition, call the `DeleteTagAsync` method on the `TagManager`:

```cs
private async Task DeleteTagAsync(string nameOrId, CancellationToken cancellationToken = default) {
    await _tagManager.DeleteTagAsync(nameOrId, cancellationToken).ConfigureAwait(false);
}
```
