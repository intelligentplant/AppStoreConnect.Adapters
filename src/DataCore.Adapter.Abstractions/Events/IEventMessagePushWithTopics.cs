using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter for specific topics via 
    /// a push notification.
    /// </summary>
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
        /// <returns>
        ///   A task that will create and start a subscription object that can be disposed once 
        ///   the subscription is no longer required.
        /// </returns>
        Task<IEventMessageSubscriptionWithTopics> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request
        );

    }

}
