using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

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
            return await adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

    }
}
