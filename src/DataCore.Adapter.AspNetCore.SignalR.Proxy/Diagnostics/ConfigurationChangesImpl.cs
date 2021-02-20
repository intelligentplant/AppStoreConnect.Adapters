using System;
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
        public Task<ChannelReader<ConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            ConfigurationChangesSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            return client.ConfigurationChanges.CreateConfigurationChangesChannelAsync(AdapterId, request, cancellationToken);
        }
    }
}
