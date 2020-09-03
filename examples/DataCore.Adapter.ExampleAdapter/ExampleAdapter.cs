using System;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter that has data source capabilities (tag search, tag value queries, etc). The 
    /// adapter contains a set of sensor-like data for 3 tags that it will loop over.
    /// </summary>
    public class ExampleAdapter : Csv.CsvAdapter  {

        private const string CsvFile = "tag-data.csv";

        private readonly Features.AssetModelBrowser _assetModelBrowser;

        /// <summary>
        /// Creates a new <see cref="ExampleAdapter"/> object.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The adapter logger.
        /// </param>
        public ExampleAdapter(IBackgroundTaskService backgroundTaskService, ILogger<ExampleAdapter> logger) : base(
            "wind-power",
            new Csv.CsvAdapterOptions() {
                Name = "Wind Power Energy Company",
                Description = "An example data source adapter for a wind farm operator",
                IsDataLoopingAllowed = true,
                SnapshotPushUpdateInterval = 5000,
                GetCsvStream = () => typeof(ExampleAdapter).Assembly.GetManifestResourceStream(typeof(ExampleAdapter), CsvFile)
            },
            backgroundTaskService,
            logger
        ) {
            // Register additional features!
            _assetModelBrowser = new Features.AssetModelBrowser(BackgroundTaskService);
            AddFeature<IAssetModelBrowse, Features.AssetModelBrowser>(_assetModelBrowser);
            AddFeatures(new InMemoryEventMessageStore(new InMemoryEventMessageManagerOptions() { Capacity = 500 }, backgroundTaskService, Logger));
            AddExtensionFeatures(new ExampleExtensionImpl(this));
        }


        protected override async Task StartAsync(CancellationToken cancellationToken) {
            var startup = DateTime.UtcNow;
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            var adapter = (IAdapter) this;
            await _assetModelBrowser.Init(adapter.Descriptor.Id, adapter.Features.Get<RealTimeData.ITagSearch>(), cancellationToken).ConfigureAwait(false);

            _ = Task.Run(async () => {
                try {
                    while (!StopToken.IsCancellationRequested) {
                        var evtManager = (InMemoryEventMessageStore) Features.Get<IWriteEventMessages>();
                        await evtManager.WriteEventMessages(
                            EventMessageBuilder
                                .Create()
                                .WithPriority(EventPriority.Low)
                                .WithCategory("System Messages")
                                .WithMessage($"Uptime: {(DateTime.UtcNow - startup)}")
                                .Build()
                        ).ConfigureAwait(false);

                        await Task.Delay(TimeSpan.FromSeconds(60), StopToken).ConfigureAwait(false);
                    }
                }
                catch { }
            });
        }


        private class ExampleExtensionImpl : AdapterExtensionFeature, IExampleExtensionFeature {

            public ExampleExtensionImpl(ExampleAdapter adapter) : base(adapter.BackgroundTaskService) {
                Bind<PingMessage, PongMessage>(Ping);
            }


            public PongMessage Ping(IAdapterCallContext context, PingMessage ping) {
                return new PongMessage() { 
                    CorrelationId = ping.CorrelationId
                };
            }

        }

    }

}
