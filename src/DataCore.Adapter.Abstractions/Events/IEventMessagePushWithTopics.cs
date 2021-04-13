using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter for specific topics via 
    /// a push notification.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Events.EventMessagePushWithTopics,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_EventMessagePushWithTopics),
        Description = nameof(AbstractionsResources.Description_EventMessagePushWithTopics)
    )]
    public interface IEventMessagePushWithTopics : IAdapterFeature {

        /// <summary>
        /// Creates a topic-based event message subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request specifying parameters for the subscription, such as whether a passive or 
        ///   active subscription should be created. Some adapters will only emit event messages 
        ///   when they have at least one active subscriber.
        /// </param>
        /// <param name="channel">
        ///   A channel that will add topics to or remove topics from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A channel reader that will emit event messages as they occur.
        /// </returns>
        IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        );

    }

}
