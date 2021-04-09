using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        public async IAsyncEnumerable<ConfigurationChange> CreateConfigurationChangesChannel(
            string adapterId,
            ConfigurationChangesSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IConfigurationChanges>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartConfigurationChangesSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }

    }
}
