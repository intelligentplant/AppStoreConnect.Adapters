using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.Services;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter that has data source capabilities (tag search, tag value queries, etc). The 
    /// adapter contains a set of sensor-like data for 3 tags that it will loop over.
    /// </summary>
    public class ExampleAdapter : Csv.CsvAdapter  {

        private const string CsvFile = "tag-data.csv";

        private readonly AssetModelManager _assetModelBrowser;

        /// <summary>
        /// Creates a new <see cref="ExampleAdapter"/> object.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="encoders">
        ///   The <see cref="IExtensionObjectEncoder"/> instances that can be used when encoding 
        ///   or decoding <see cref="EncodedObject"/> instances.
        /// </param>
        /// <param name="logger">
        ///   The adapter logger.
        /// </param>
        public ExampleAdapter(IBackgroundTaskService backgroundTaskService, IEnumerable<IObjectEncoder> encoders, ILogger<ExampleAdapter> logger) : base(
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
            _assetModelBrowser = new AssetModelManager(new InMemoryKeyValueStore(), BackgroundTaskService);

            AddFeatures(_assetModelBrowser);
            AddFeatures(new InMemoryEventMessageStore(new InMemoryEventMessageStoreOptions() { Capacity = 500 }, backgroundTaskService, Logger));
            AddExtensionFeatures(new ExampleExtensionImpl(this, encoders));
        }


        /// <inheritdoc/>
        protected override async Task OnStartedAsync(CancellationToken cancellationToken) {
            var startup = DateTime.UtcNow;
            await InitialiseAssetModelAsync(cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested) {
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


        private async Task InitialiseAssetModelAsync(CancellationToken cancellationToken) {
            await _assetModelBrowser.InitAsync(cancellationToken).ConfigureAwait(false);

            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ExampleAdapter), "asset-model.json"))
            using (var reader = new System.IO.StreamReader(stream)) {
                var json = reader.ReadToEnd();
                var nodeDefinitions = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetModelNodeDefinition[]>(json);

                var dataReferences = nodeDefinitions.Where(x => !string.IsNullOrWhiteSpace(x.DataReference)).Select(x => x.DataReference).ToArray();

                var tagSearch = this.GetFeature<ITagSearch>();
                var dataReferencesChannel = tagSearch.GetTags(new DefaultAdapterCallContext(), new Tags.GetTagsRequest() {
                    Tags = dataReferences
                }, cancellationToken);

                var tags = await dataReferencesChannel.ToEnumerable(cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var nodeDefinition in nodeDefinitions) {
                    await _assetModelBrowser.AddOrUpdateNodeAsync(
                        new AssetModelNodeBuilder()
                            .WithId(nodeDefinition.Id)
                            .WithName(nodeDefinition.Name)
                            .WithNodeType(nodeDefinition.NodeType)
                            .WithDescription(nodeDefinition.Description)
                            .WithParent(nodeDefinition.Parent)
                            .WithChildren(nodeDefinition.Children?.Any() ?? false)
                            .WithDataReference(string.IsNullOrWhiteSpace(nodeDefinition.DataReference)
                                ? null
                                : new DataReference(
                                    Descriptor.Id,
                                    tags.First(t => t.Id.Equals(nodeDefinition.DataReference, StringComparison.Ordinal) || t.Name.Equals(nodeDefinition.DataReference, StringComparison.Ordinal)).Name
                                )
                            )
                            .Build(), 
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
        }


#pragma warning disable CS0618 // Type or member is obsolete
        internal class ExampleExtensionImpl : AdapterExtensionFeature, IExampleExtensionFeature {

            public ExampleExtensionImpl(ExampleAdapter adapter, IEnumerable<IObjectEncoder> encoders) : base(adapter.BackgroundTaskService, encoders) {
                BindInvoke<IExampleExtensionFeature, string, InvocationResponse>(Ping);
            }


            public InvocationResponse Ping(IAdapterCallContext context, string correlationId) {
                return new InvocationResponse() { 
                    Results = new Variant[] {
                        correlationId,
                        DateTime.UtcNow
                    }
                };
            }

            
            internal static ExtensionFeatureOperationDescriptorPartial GetPingDescriptor() {
                return new ExtensionFeatureOperationDescriptorPartial() {
                    Name = "Ping",
                    Description = "Responds to a ping message with a pong message",
                    Inputs = new[] {
                        new ExtensionFeatureOperationParameterDescriptor() {
                            Ordinal = 0,
                            VariantType = VariantType.String,
                            Description = "The correlation ID for the ping message"
                        }
                    },
                    Outputs = new[] {
                        new ExtensionFeatureOperationParameterDescriptor() {
                            Ordinal = 0,
                            VariantType = VariantType.String,
                            Description = "The correlation ID for the pong message"
                        },
                        new ExtensionFeatureOperationParameterDescriptor() {
                            Ordinal = 1,
                            VariantType = VariantType.DateTime,
                            Description = "The UTC time that the ping request was processed at"
                        }
                    }
                };
            }

        }
#pragma warning restore CS0618 // Type or member is obsolete

    }

}
