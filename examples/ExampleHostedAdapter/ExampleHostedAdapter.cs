
using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Services;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Options;


namespace ExampleHostedAdapter {

    // This is your adapter class. For information about how to add features to your adapter (such 
    // as tag browsing, or real-time data queries), visit https://github.com/intelligentplant/AppStoreConnect.Adapters.

    // The [AdapterMetadata] attribute is used to provide information about your adapter type at
    // runtime.
    [AdapterMetadata(
        // This is a URI to identify the adapter type; it is not required that the URI can be
        // dereferenced!
        "https://my-company.com/app-store-connect/adapters/my-adapter/",
        // The display name for the adapter type.
        Name = "My Adapter",
        // The adapter type description.
        Description = "A brief description of the adapter type"
    )]
    public partial class ExampleHostedAdapter : AdapterBase<ExampleHostedAdapterOptions> {

        private static readonly AdapterProperty s_tagCreatedAtPropertyDefinition = new AdapterProperty("UTC Created At", DateTime.MinValue, "The UTC creation time for the tag");

        private readonly TagManager _tagManager;

        private readonly PollingSnapshotTagValuePush _snapshotPush;


        public ExampleHostedAdapter(
            string id, 
            IOptionsMonitor<ExampleHostedAdapterOptions> options,
            IKeyValueStore keyValueStore,
            IBackgroundTaskService taskScheduler,
            ILogger<ExampleHostedAdapter> logger
        ) : base(id, options, taskScheduler, logger) {

            // The TagManager implements the ITagSearch adapter feature on our adapter's behalf,
            // meaning that our adapter allows callers to discover available tags (measurements)
            // that can be read. In our example we use a fixed set of tags created at startup time,
            // but your implementation might e.g. query a database to get a list of available
            // measurements. In this circumstance, you can implement ITagSearch directly instead
            // of using the TagManager.
            //
            // See https://github.com/intelligentplant/AppStoreConnect.Adapters for more details.
            _tagManager = new TagManager(
                // We can persist tag definitions between restarts using the provided IKeyValueStore
                // service. The CreateScopedStore extension method adds a prefix to all keys that
                // we read from or write to, which is useful if multiple services in your
                // application are sharing the same store.
                //
                // If we are not interested in persisting tag definitions, we can pass null
                // here instead.
                keyValueStore.CreateScopedStore(id),
                BackgroundTaskService,
                new[] { s_tagCreatedAtPropertyDefinition }
            );

            // Tell the adapter to advertise that it supports all of the adapter features
            // implemented by the TagManager object.
            AddFeatures(_tagManager);

            // The PollingSnapshotTagValuePush class implements the ISnapshotTagValuePush, meaning
            // that callers can subscribe to be notified of snapshot value changes. Under the hood,
            // PollingSnapshotTagValuePushOptions functions by periocially polling the snapshot
            // value for tags with subscribers. If your adapter receives push notifications of new
            // values from an external source (such as an MQTT broker), you can use the
            // SnapshotTagValuePush class instead, and pass new values to it as they arrive.
            //
            // See https://github.com/intelligentplant/AppStoreConnect.Adapters for more details.
            _snapshotPush = new PollingSnapshotTagValuePush(this, new PollingSnapshotTagValuePushOptions() { 
                AdapterId = Descriptor.Id,
                PollingInterval = TimeSpan.FromSeconds(5),
                TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
            }, BackgroundTaskService, Logger);

            // Tell the adapter to advertise that it supports all of the adapter features
            // implemented by the PollingSnapshotTagValuePush object.
            AddFeatures(_snapshotPush);
        }


        // The StartAsync method is called when the adapter is being started up. Use this method to 
        // initialise any required connections to external systems (e.g. connecting to a database, 
        // MQTT broker, industrial plant historian, etc).
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            // Initialise our tag manager and register a test tag.
            await _tagManager.InitAsync(cancellationToken).ConfigureAwait(false);

            var testTag = new TagDefinitionBuilder("test", "Test Tag")
                .WithDescription("An example tag that can be polled for snapshot (current) values.")
                .WithDataType(VariantType.Double)
                .WithSupportsReadSnapshotValues()
                .WithProperty(s_tagCreatedAtPropertyDefinition.Name, DateTime.UtcNow)
                .Build();

            await _tagManager.AddOrUpdateTagAsync(testTag, cancellationToken).ConfigureAwait(false);
        }


        // The StopAsync method is called when the adapter is being shut down. Use this method to 
        // shut down connections to external systems. Note that an adapter can be stopped and 
        // started again without being disposed. You should override the Dispose and DisposeAsync 
        // methods to dispose of resources when the adapter is being disposed.
        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        // The OnOptionsChange method is called every time the adapter receives an update to its
        // options at runtime. You can use this method to trigger any runtime changes required.
        // You can test this functionality by running the application and then changing the
        // adapter name or description in appsettings.json at runtime.
        protected override void OnOptionsChange(ExampleHostedAdapterOptions options) {
            base.OnOptionsChange(options);
        }


        // Override the CheckHealthAsync method to add custom health checks to your adapter. 
        // Health checks allow you to report on the status of e.g. connections to external 
        // systems. If you detect that the underlying health status of the adapter has changed 
        // (e.g. you unexpectedly disconnect from an external system) you can notify the base 
        // class that the overall health status must be recalculated by calling the 
        // OnHealthStatusChanged method.
        protected override async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(
            IAdapterCallContext context, 
            CancellationToken cancellationToken
        ) {
            var result = new List<HealthCheckResult>();
            result.AddRange(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));

            // Use the IsRunning flag to detect if the adapter has been initialised.

            if (!IsRunning) {
                return result;
            }

            // Add custom health check results to the list.

            return result;
        }


        // Your adapter implements both IDisposable and IAsyncDisposable.
        // 
        // Override the Dispose(bool) and DisposeAsyncCore() methods if you need to dispose of
        // managed or unmanaged resources. You do not need to manually dispose of any object that
        // has been registered with the adapter as a feature provider.
        //
        // When IDisposable.Dispose() is called on your adapter, Dispose(true) will be called.
        //
        // When IAsyncDisposable.DisposeAsync() is called on your adapter, DisposeAsyncCore()
        // is called, followed by Dispose(false). This is the standard pattern for implementing
        // both IDisposable and IAsyncDisposable on the same type. See here for more details: 
        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync

    }
}
