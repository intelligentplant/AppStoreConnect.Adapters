using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Diagnostics;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for configuration change queries.

    public partial class AdapterHub {

        /// <summary>
        /// Creates a channel that will receive configuration changes from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that subscribers can observe to receive configuration change 
        ///   notifications.
        /// </returns>
        public async Task<ChannelReader<ConfigurationChange>> CreateConfigurationChangesChannel(
            string adapterId,
            ConfigurationChangesSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IConfigurationChanges>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateChannel<ConfigurationChange>(DefaultChannelCapacity);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartConfigurationChangesSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.Subscribe(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
