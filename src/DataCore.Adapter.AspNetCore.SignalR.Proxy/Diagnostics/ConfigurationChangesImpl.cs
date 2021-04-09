using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData {

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
        public ConfigurationChangesImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async IAsyncEnumerable<ConfigurationChange> Subscribe(
            IAdapterCallContext context, 
            ConfigurationChangesSubscriptionRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.ConfigurationChanges.CreateConfigurationChangesChannelAsync(AdapterId, request, ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }
    }
}
