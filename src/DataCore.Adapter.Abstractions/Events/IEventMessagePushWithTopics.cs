using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter for specific topics via 
    /// a push notification.
    /// </summary>
    [AdapterFeature(WellKnownFeatures.Events.EventMessagePushWithTopics)]
    public interface IEventMessagePushWithTopics : IAdapterFeature {

        /// <summary>
        /// Creates a push subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request specifying parameters for the subscription, such as whether a passive or 
        ///   active subscription should be created. Some adapters will only emit event messages 
        ///   when they have at least one active subscriber.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A channel reader that will emit event messages as they occur.
        /// </returns>
        Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            CancellationToken cancellationToken
        );

    }

}
