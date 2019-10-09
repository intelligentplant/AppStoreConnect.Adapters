using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Features;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class AssetModelBrowserServiceImpl : AssetModelBrowserService.AssetModelBrowserServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public AssetModelBrowserServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task BrowseAssetModelNodes(BrowseAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowser>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.Models.BrowseAssetModelNodesRequest() {
                ParentId = string.IsNullOrWhiteSpace(request.ParentId) 
                    ? null 
                    : request.ParentId,
                PageSize = request.PageSize,
                Page = request.Page
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.BrowseAssetModelNodes(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var node) || node == null) {
                    continue;
                }

                await responseStream.WriteAsync(node.ToGrpcAssetModelNode()).ConfigureAwait(false);
            }
        }


        public override async Task GetAssetModelNodes(GetAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowser>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.Models.GetAssetModelNodesRequest() {
                Nodes = request.Nodes.ToArray()
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.GetAssetModelNodes(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var node) || node == null) {
                    continue;
                }

                await responseStream.WriteAsync(node.ToGrpcAssetModelNode()).ConfigureAwait(false);
            }
        }


        public override async Task FindAssetModelNodes(FindAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowser>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.Models.FindAssetModelNodesRequest() {
                Name = request.Name,
                Description = request.Description,
                PageSize = request.PageSize,
                Page = request.Page
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.FindAssetModelNodes(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var node) || node == null) {
                    continue;
                }

                await responseStream.WriteAsync(node.ToGrpcAssetModelNode()).ConfigureAwait(false);
            }
        }

    }
}
