using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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


        internal async Task Init(string adapterId, Tags.ITagSearch tagSearch, CancellationToken cancellationToken) {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ExampleAdapter), AssetModelJson))
            using (var reader = new System.IO.StreamReader(stream)) {
                var json = reader.ReadToEnd();
                var nodeDefinitions = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetModelNodeDefinition[]>(json);

                var dataReferences = nodeDefinitions.Where(x => !string.IsNullOrWhiteSpace(x.DataReference)).Select(x => x.DataReference).ToArray();

                var dataReferencesChannel = tagSearch.GetTags(new DefaultAdapterCallContext(), new Tags.GetTagsRequest() { 
                    Tags = dataReferences
                }, cancellationToken);

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
                                tags.First(t => t.Id.Equals(x.DataReference, StringComparison.Ordinal) || t.Name.Equals(x.DataReference, StringComparison.Ordinal)).Name
                            )
                        )
                        .Build()
                ).ToDictionary(x => x.Id);
            }
        }


        public async IAsyncEnumerable<AssetModelNode> BrowseAssetModelNodes(
            IAdapterCallContext context, 
            BrowseAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await Task.CompletedTask.ConfigureAwait(false);

            var nodes = string.IsNullOrWhiteSpace(request.ParentId)
                ? _nodes.Values.Where(x => string.IsNullOrWhiteSpace(x.Parent))
                : _nodes.Values.Where(x => string.Equals(x.Parent, request.ParentId, StringComparison.Ordinal));

            foreach (var node in nodes.ApplyFilter(request)) {
                yield return node;
            }
        }


        public async IAsyncEnumerable<AssetModelNode> GetAssetModelNodes(
            IAdapterCallContext context, 
            GetAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await Task.CompletedTask.ConfigureAwait(false);

            foreach (var item in request.Nodes) {
                if (!_nodes.TryGetValue(item, out var node)) {
                    continue;
                }

                yield return node;
            }
        }


        public async IAsyncEnumerable<AssetModelNode> FindAssetModelNodes(
            IAdapterCallContext context, 
            FindAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await Task.CompletedTask.ConfigureAwait(false);

            IEnumerable<AssetModelNode> nodes = _nodes.Values.ApplyFilter(request);

            foreach (var node in nodes) {
                yield return node;
            }
        }
    }
}
