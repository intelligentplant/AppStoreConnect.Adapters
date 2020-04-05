using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="AssetModelBrowserService.AssetModelBrowserServiceBase"/>
    /// </summary>
    public class AssetModelBrowserServiceImpl : AssetModelBrowserService.AssetModelBrowserServiceBase {

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the caller.
        /// </summary>
        private readonly IAdapterCallContext _adapterCallContext;

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterCallContext">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public AssetModelBrowserServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task BrowseAssetModelNodes(BrowseAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowse>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.BrowseAssetModelNodesRequest() {
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


        /// <inheritdoc/>
        public override async Task GetAssetModelNodes(GetAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowse>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.GetAssetModelNodesRequest() {
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


        /// <inheritdoc/>
        public override async Task FindAssetModelNodes(FindAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelSearch>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.FindAssetModelNodesRequest() {
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
