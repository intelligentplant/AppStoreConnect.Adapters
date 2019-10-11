using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.Events.Features;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter that has data source capabilities (tag search, tag value queries, etc). The 
    /// adapter contains a set of sensor-like data for 3 tags that it will loop over.
    /// </summary>
    public class ExampleAdapter : Csv.CsvAdapter, IExampleExtensionFeature  {

        private const string CsvFile = "tag-data.csv";

        private readonly Features.AssetModelBrowser _assetModelBrowser;

        /// <summary>
        /// Creates a new <see cref="ExampleAdapter"/> object.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The adapter logger factory.
        /// </param>
        public ExampleAdapter(ILoggerFactory loggerFactory) : base(
            new Csv.CsvAdapterOptions() {
                Id = "wind-power",
                Name = "Wind Power Energy Company",
                Description = "An example data source adapter for a wind farm operator",
                IsDataLoopingAllowed = true,
                SnapshotPushUpdateInterval = 5000,
                GetCsvStream = () => typeof(ExampleAdapter).Assembly.GetManifestResourceStream(typeof(ExampleAdapter), CsvFile)
            },
            loggerFactory
        ) {
            // Register additional features!
            _assetModelBrowser = new Features.AssetModelBrowser();
            AddFeature<IAssetModelBrowse, Features.AssetModelBrowser>(_assetModelBrowser);
            AddFeature<IEventMessagePush, EventsSubscriptionManager>(new EventsSubscriptionManager(TimeSpan.FromSeconds(60)));
        }


        DateTime IExampleExtensionFeature.GetCurrentTime() {
            return DateTime.UtcNow;
        }


        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            var adapter = (IAdapter) this;
            await _assetModelBrowser.Init(adapter.Descriptor.Id, adapter.Features.Get<RealTimeData.Features.ITagSearch>(), cancellationToken).ConfigureAwait(false);
        }


        private class EventsSubscriptionManager : Events.Utilities.EventMessageSubscriptionManager {

            private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();


            internal EventsSubscriptionManager(TimeSpan interval) : base(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance) {
                var startup = DateTime.UtcNow;
                _ = Task.Run(async () => {
                    try {
                        while (!_disposedTokenSource.IsCancellationRequested) {
                            await Task.Delay(interval, _disposedTokenSource.Token).ConfigureAwait(false);
                            OnMessage(Events.Models.EventMessageBuilder
                                .Create()
                                .WithPriority(Events.Models.EventPriority.Low)
                                .WithCategory("System Messages")
                                .WithMessage($"Uptime: {(DateTime.UtcNow - startup)}")
                                .Build()
                            );
                        }
                    }
                    catch { }
                });
            }


            protected override Task OnSubscriptionAdded(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            protected override Task OnSubscriptionRemoved(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _disposedTokenSource.Cancel();
                    _disposedTokenSource.Dispose();
                }
            }

        }

    }

}
