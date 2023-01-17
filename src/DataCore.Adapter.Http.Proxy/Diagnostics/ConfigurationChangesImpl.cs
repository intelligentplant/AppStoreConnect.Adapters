using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter.Http.Proxy.Diagnostics {

    /// <summary>
    /// Implements <see cref="IConfigurationChanges"/>.
    /// </summary>
    internal class ConfigurationChangesImpl : ProxyAdapterFeature, IConfigurationChanges {

        /// <summary>
        /// Creates a new <see cref="ConfigurationChangesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ConfigurationChangesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<ConfigurationChange> Subscribe(
            IAdapterCallContext context,
            ConfigurationChangesSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetSignalRClient(context);
            await client.StreamStartedAsync().ConfigureAwait(false);

            try {
                await foreach (var item in client.Client.ConfigurationChanges.CreateConfigurationChangesChannelAsync(AdapterId, request, cancellationToken).ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }
            }
            finally {
                await client.StreamCompletedAsync().ConfigureAwait(false);
            }
        }
    }

}
