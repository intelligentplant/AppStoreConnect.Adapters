using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.AssetModel.Models;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.ApiTestAdapter.Features {
    internal class AssetModelBrowserImpl : IAssetModelBrowser {

        private readonly AssetModelNode[] _nodes;


        internal AssetModelBrowserImpl(ApiTestAdapter adapter) {
            _nodes = ConfigureNodes(((IAdapter) adapter).Descriptor.Id);
        }


        public ChannelReader<AssetModelNode> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel(0);

            result.Writer.RunBackgroundOperation((ch, ct) => {
                var node = request.ParentId == null
                    ? _nodes.First()
                    : _nodes.FirstOrDefault(x => x.Id.Equals(request.ParentId));

                if (node == null) {
                    return;
                }

                var depth = 1;
                var @continue = false;

                do {
                    ch.TryWrite(node);
                } while (@continue);

            }, true, cancellationToken);

            return result;
        }


        public ChannelReader<AssetModelNode> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel(0);

            result.Writer.RunBackgroundOperation((ch, ct) => { }, true, cancellationToken);

            return result;
        }


        public ChannelReader<AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel(0);

            result.Writer.RunBackgroundOperation((ch, ct) => {
                IEnumerable<AssetModelNode> nodes = _nodes;
                if (!string.IsNullOrWhiteSpace(request.Name) && !string.Equals(request.Name, "*")) {
                    nodes = nodes.Where(x => x.Name.Like(request.Name));
                }
                if (!string.IsNullOrWhiteSpace(request.Description) && !string.Equals(request.Description, "*")) {
                    nodes = nodes.Where(x => x.Description.Like(request.Description));
                }

                nodes = nodes.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);

                foreach (var node in nodes) {
                    ch.TryWrite(node);
                }
            }, true, cancellationToken);

            return result;
        }


        private AssetModelNode[] ConfigureNodes(string adapterId) {
            return new[] {
                new AssetModelNode(
                    "1",
                    "Level 1",
                    "The top-most node in the hierarchy.",
                    null,
                    new [] { "2", "3" },
                    null,
                    null
                ),
                new AssetModelNode(
                    "2",
                    "Level 1_1",
                    "A second-level node.",
                    "1",
                    new [] { "4", "5" },
                    new [] {
                        new AssetModelNodeMeasurement(
                            "Measurement 1",
                            adapterId,
                            new TagIdentifier("id", "name")
                        )
                    },
                    null
                ),
                new AssetModelNode(
                    "3",
                    "Level 1_2",
                    "A second-level node.",
                    "1",
                    new [] { "6" },
                    new [] {
                        new AssetModelNodeMeasurement(
                            "Measurement 1",
                            adapterId,
                            new TagIdentifier("id", "name")
                        )
                    },
                    null
                ),
                new AssetModelNode(
                    "4",
                    "Level 1_1_1",
                    "A third-level node.",
                    "2",
                    null,
                    new [] {
                        new AssetModelNodeMeasurement(
                            "Measurement 1",
                            adapterId,
                            new TagIdentifier("id", "name")
                        ),
                        new AssetModelNodeMeasurement(
                            "Measurement 2",
                            adapterId,
                            new TagIdentifier("id", "name")
                        )
                    },
                    null
                ),
                new AssetModelNode(
                    "5",
                    "Level 1_1_2",
                    "A third-level node.",
                    "2",
                    null,
                    new [] {
                        new AssetModelNodeMeasurement(
                            "Measurement 1",
                            adapterId,
                            new TagIdentifier("id", "name")
                        ),
                        new AssetModelNodeMeasurement(
                            "Measurement 2",
                            adapterId,
                            new TagIdentifier("id", "name")
                        )
                    },
                    null
                ),
                new AssetModelNode(
                    "6",
                    "Level 1_2_1",
                    "A third-level node.",
                    "3",
                    null,
                    new [] {
                        new AssetModelNodeMeasurement(
                            "Measurement 1",
                            adapterId,
                            new TagIdentifier("id", "name")
                        ),
                        new AssetModelNodeMeasurement(
                            "Measurement 2",
                            adapterId,
                            new TagIdentifier("id", "name")
                        )
                    },
                    null
                )
            };
        }

    }
}
