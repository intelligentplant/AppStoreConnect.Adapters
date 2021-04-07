using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.AssetModel;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for asset model browsing.

    public partial class AdapterHub {

        /// <summary>
        /// Browses nodes in an adapter's asset model hierarchy.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async Task<ChannelReader<AssetModelNode>> BrowseAssetModelNodes(string adapterId, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartBrowseAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.BrowseAssetModelNodes(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }


        /// <summary>
        /// Gets nodes in an adapter's asset model hierarchy by ID.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async Task<ChannelReader<AssetModelNode>> GetAssetModelNodes(string adapterId, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartGetAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.GetAssetModelNodes(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }


        /// <summary>
        /// Finds nodes in an adapter's asset model hierarchy that match the specified search filters.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async Task<ChannelReader<AssetModelNode>> FindAssetModelNodes(string adapterId, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelSearch>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartFindAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.FindAssetModelNodes(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
