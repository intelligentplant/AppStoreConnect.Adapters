using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.Json;

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
        /// <param name="taskScheduler">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The adapter logger.
        /// </param>
        public ExampleAdapter(IBackgroundTaskService taskScheduler, ILogger<ExampleAdapter> logger) : base(
            "wind-power",
            new Csv.CsvAdapterOptions() {
                Name = "Wind Power Energy Company",
                Description = "An example data source adapter for a wind farm operator",
                IsDataLoopingAllowed = true,
                SnapshotPushUpdateInterval = 5000,
                GetCsvStream = () => typeof(ExampleAdapter).Assembly.GetManifestResourceStream(typeof(ExampleAdapter), CsvFile)
            },
            taskScheduler,
            logger
        ) {
            // Register additional features!
            _assetModelBrowser = new Features.AssetModelBrowser(TaskScheduler);
            AddFeature<IAssetModelBrowse, Features.AssetModelBrowser>(_assetModelBrowser);
            AddFeatures(new InMemoryEventMessageStore(new InMemoryEventMessageManagerOptions() { Capacity = 500 }, taskScheduler, Logger));
            AddFeature<IExampleExtensionFeature, ExampleExtensionFeatureImpl>(new ExampleExtensionFeatureImpl());
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


        private class ExampleExtensionFeatureImpl : AdapterExtensionFeature, IExampleExtensionFeature {

            private static readonly ExtensionFeatureOperationDescriptor s_getCurrentTime = new ExtensionFeatureOperationDescriptor() { 
                OperationId = GetOperationUri<IExampleExtensionFeature>(nameof(GetCurrentTime)),
                OperationType = ExtensionFeatureOperationType.Invoke,
                Name = nameof(GetCurrentTime),
                Description = "Gets the current UTC time",
                Output = new ExtensionFeatureOperationParameterDescriptor() {
                    Description = "ISO 8601 timestamp",
                    ExampleValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                }
            };


            private static readonly IEnumerable<ExtensionFeatureOperationDescriptor> s_operations = new[] {
                s_getCurrentTime
            };


            public DateTime GetCurrentTime() {
                return DateTime.UtcNow;
            }


            protected override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(IAdapterCallContext context, Uri featureUri, CancellationToken cancellationToken) {
                return Task.FromResult(s_operations);
            }


            protected override Task<string> Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
                if (s_getCurrentTime.OperationId.Equals(operationId)) {
                    return Task.FromResult(GetCurrentTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                }
                
                return base.Invoke(context, operationId, argument, cancellationToken);
            }

        }

    }

}
