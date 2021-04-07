using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Tags;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for tag search queries.

    public partial class AdapterHub {

        /// <summary>
        /// Performs a tag search.
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
        ///   The matching tags.
        /// </returns>
        public async Task<ChannelReader<TagDefinition>> FindTags(string adapterId, FindTagsRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagSearch>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartFindTagsActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.FindTags(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }


        /// <summary>
        /// Gets tags by ID or name.
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
        ///   The matching tags.
        /// </returns>
        public async Task<ChannelReader<TagDefinition>> GetTags(string adapterId, GetTagsRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartGetTagsActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.GetTags(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }


        /// <summary>
        /// Gets tag property definitions.
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
        ///   The matching tags.
        /// </returns>
        public async Task<ChannelReader<AdapterProperty>> GetTagProperties(string adapterId, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateChannel<AdapterProperty>(DefaultChannelCapacity);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartGetTagPropertiesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.GetTagProperties(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
