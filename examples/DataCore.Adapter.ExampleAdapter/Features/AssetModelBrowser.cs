using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.AssetModel.Models;

namespace DataCore.Adapter.Example.Features {
    internal class AssetModelBrowser : IAssetModelBrowse, IAssetModelSearch {

        private const string AssetModelJson = "asset-model.json";

        private IDictionary<string, AssetModelNode> _nodes;


        internal async Task Init(string adapterId, RealTimeData.Features.ITagSearch tagSearch, CancellationToken cancellationToken) {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ExampleAdapter), AssetModelJson))
            using (var reader = new System.IO.StreamReader(stream)) {
                var json = reader.ReadToEnd();
                var nodeDefinitions = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetModelNodeDefinition[]>(json);

                var tagIdsOrNames = nodeDefinitions.SelectMany(x => x.Measurements.Select(m => m.Tag)).ToArray();
                var tagsChannel = tagSearch.GetTags(null, new RealTimeData.Models.GetTagsRequest() { 
                    Tags = tagIdsOrNames
                }, cancellationToken);

                var tags = await tagsChannel.ReadItems(cancellationToken: cancellationToken).ConfigureAwait(false);

                _nodes = nodeDefinitions.Select(x => AssetModelNode.Create(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.Parent,
                    x.Children,
                    x.Measurements.Select(m => {
                        var tag = tags.FirstOrDefault(t => string.Equals(t.Id, m.Tag, StringComparison.Ordinal) || string.Equals(t.Name, m.Tag, StringComparison.Ordinal));
                        if (tag == null) {
                            return null;
                        }
                        return AssetModelNodeMeasurement.Create(
                            m.Name,
                            adapterId,
                            RealTimeData.Models.TagSummary.Create(tag.Id, tag.Name, tag.Description, tag.Units)
                        );
                    }).Where(m => m != null),
                    x.Properties
                )).ToDictionary(x => x.Id);
            }
        }


        public ChannelReader<AssetModelNode> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var channel = ChannelExtensions.CreateAssetModelNodeChannel();

            channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                var skipCount = (request.Page - 1) * request.PageSize;
                var takeCount = request.PageSize;

                var nodes = string.IsNullOrWhiteSpace(request.ParentId)
                    ? _nodes.Values.Where(x => string.IsNullOrWhiteSpace(x.Parent))
                    : _nodes.Values.Where(x => string.Equals(x.Parent, request.ParentId, StringComparison.Ordinal));

                BrowseAssetModelNodes(nodes, ch, ref skipCount, ref takeCount);

            }, true, cancellationToken);

            return channel;
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


        public ChannelReader<AssetModelNode> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var channel = ChannelExtensions.CreateAssetModelNodeChannel();

            channel.Writer.RunBackgroundOperation((ch, ct) => {
                foreach (var item in request.Nodes) {
                    if (!_nodes.TryGetValue(item, out var node)) {
                        continue;
                    }

                    ch.TryWrite(node);
                }
            }, true, cancellationToken);

            return channel;
        }


        public ChannelReader<AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
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
            }, true, cancellationToken);

            return channel;
        }
    }
}
