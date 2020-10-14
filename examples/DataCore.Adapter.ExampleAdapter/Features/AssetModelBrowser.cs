using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Example.Features {
    internal class AssetModelBrowser : IAssetModelBrowse, IAssetModelSearch {

        private const string AssetModelJson = "asset-model.json";

        private IDictionary<string, AssetModelNode> _nodes;

        public IBackgroundTaskService BackgroundTaskService { get; }


        internal AssetModelBrowser(IBackgroundTaskService backgroundTaskService) {
            BackgroundTaskService = backgroundTaskService ?? throw new ArgumentNullException(nameof(backgroundTaskService));

        }


        internal async Task Init(string adapterId, RealTimeData.ITagSearch tagSearch, CancellationToken cancellationToken) {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ExampleAdapter), AssetModelJson))
            using (var reader = new System.IO.StreamReader(stream)) {
                var json = reader.ReadToEnd();
                var nodeDefinitions = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetModelNodeDefinition[]>(json);

                var dataReferences = nodeDefinitions.Where(x => !string.IsNullOrWhiteSpace(x.DataReference)).Select(x => x.DataReference).ToArray();

                var dataReferencesChannel = await tagSearch.GetTags(null, new RealTimeData.GetTagsRequest() { 
                    Tags = dataReferences
                }, cancellationToken).ConfigureAwait(false);

                var tags = await dataReferencesChannel.ToEnumerable(cancellationToken: cancellationToken).ConfigureAwait(false);

                _nodes = nodeDefinitions.Select(x => 
                    AssetModelNodeBuilder
                        .Create()
                        .WithId(x.Id)
                        .WithName(x.Name)
                        .WithNodeType(x.NodeType)
                        .WithDescription(x.Description)
                        .WithParent(x.Parent)
                        .WithChildren(x.Children?.Any() ?? false)
                        .WithDataReference(string.IsNullOrWhiteSpace(x.DataReference)
                            ? null
                            : new DataReference(
                                adapterId, 
                                tags.First(t => t.Id.Equals(x.DataReference, StringComparison.Ordinal) || t.Name.Equals(x.DataReference, StringComparison.Ordinal))
                            )
                        )
                        .Build()
                ).ToDictionary(x => x.Id);
            }
        }


        public Task<ChannelReader<AssetModelNode>> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var channel = ChannelExtensions.CreateAssetModelNodeChannel();

            channel.Writer.RunBackgroundOperation((ch, ct) => {
                var skipCount = (request.Page - 1) * request.PageSize;
                var takeCount = request.PageSize;

                var nodes = string.IsNullOrWhiteSpace(request.ParentId)
                    ? _nodes.Values.Where(x => string.IsNullOrWhiteSpace(x.Parent))
                    : _nodes.Values.Where(x => string.Equals(x.Parent, request.ParentId, StringComparison.Ordinal));

                BrowseAssetModelNodes(nodes, ch, ref skipCount, ref takeCount);

                return Task.CompletedTask;
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(channel.Reader);
        }


        private void BrowseAssetModelNodes(IEnumerable<AssetModelNode> nodes, ChannelWriter<AssetModelNode> channel, ref int skip, ref int take) {
            foreach (var item in nodes) {
                if (take < 1) {
                    break;
                }

                if (skip > 0) {
                    --skip;
                    continue;
                }

                if (take > 0) {
                    channel.TryWrite(item);
                    --take;
                }
            }
        }


        public Task<ChannelReader<AssetModelNode>> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var channel = ChannelExtensions.CreateAssetModelNodeChannel();

            channel.Writer.RunBackgroundOperation((ch, ct) => {
                foreach (var item in request.Nodes) {
                    if (!_nodes.TryGetValue(item, out var node)) {
                        continue;
                    }

                    ch.TryWrite(node);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(channel.Reader);
        }


        public Task<ChannelReader<AssetModelNode>> FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var channel = ChannelExtensions.CreateAssetModelNodeChannel();

            channel.Writer.RunBackgroundOperation((ch, ct) => {
                IEnumerable<AssetModelNode> nodes = _nodes.Values;
                if (!string.IsNullOrWhiteSpace(request.Name)) {
                    nodes = nodes.Where(x => x.Name.Like(request.Name));
                }
                if (!string.IsNullOrWhiteSpace(request.Description)) {
                    nodes = nodes.Where(x => x.Description.Like(request.Description));
                }

                nodes = nodes.OrderBy(x => x.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);

                foreach (var node in nodes) {
                    ch.TryWrite(node);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(channel.Reader);
        }
    }
}
