using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

using IntelligentPlant.BackgroundTasks;

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
            CancellationToken cancellationToken
        ) {
            SignalRAdapterProxy.ValidateObject(request);

            return GetClient().Events.CreateEventMessageTopicChannelAsync(
                AdapterId, 
                request, 
                cancellationToken
            );
        }

    }
}
