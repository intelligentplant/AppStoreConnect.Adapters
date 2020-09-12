using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events {

    /// <summary>
    /// Implements <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    internal class EventMessagePushWithTopicsImpl : ProxyAdapterFeature, IEventMessagePushWithTopics {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopicsImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public EventMessagePushWithTopicsImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageTopicSubscriptionRequest request,
            ChannelReader<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            SignalRAdapterProxy.ValidateObject(request);

            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            return GetClient().Events.CreateEventMessageTopicChannelAsync(
                AdapterId, 
                request, 
                channel,
                cancellationToken
            );
        }

    }
}
