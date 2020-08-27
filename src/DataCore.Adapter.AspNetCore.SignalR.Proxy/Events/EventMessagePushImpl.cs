using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

    /// <summary>
    /// Implements <see cref="IEventMessagePush"/>.
    /// </summary>
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public EventMessagePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            SignalRAdapterProxy.ValidateObject(request);

            return GetClient().Events.CreateEventMessageChannelAsync(
                AdapterId,
                request,
                cancellationToken
            );
        }

    }
}
