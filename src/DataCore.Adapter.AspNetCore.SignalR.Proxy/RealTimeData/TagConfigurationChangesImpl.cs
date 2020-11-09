using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData {

    /// <summary>
    /// Implements <see cref="ITagConfigurationChanges"/>.
    /// </summary>
    internal class TagConfigurationChangesImpl : ProxyAdapterFeature, ITagConfigurationChanges {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationChangesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public TagConfigurationChangesImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<TagConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            TagConfigurationChangesSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            SignalRAdapterProxy.ValidateObject(request);

            var client = GetClient();
            return client.TagConfiguration.CreateTagConfigurationChangesChannelAsync(AdapterId, request, cancellationToken);
        }
    }
}
