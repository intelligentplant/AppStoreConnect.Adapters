using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter via a push notification.
    /// </summary>
    public interface IEventMessagePush : IAdapterFeature {

        /// <summary>
        /// Creates a push subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="subscriptionType">
        ///   Specifies if the adapter should treat this as an active or passive subscription. 
        ///   Some adapters will only emit event messages when they have at least one active 
        ///   subscriber.
        /// </param>
        /// <returns>
        ///   A task that will create and start a subscription object that can be disposed once 
        ///   the subscription is no longer required.
        /// </returns>
        /// <remarks>
        ///   When the <see cref="IEventMessageSubscription"/> is created, it must immediately 
        ///   publish a message to the subscriber to indicate that the subscription is operational. 
        ///   It is the responsibility of the subscriber to read and discard this message.
        /// </remarks>
        Task<IEventMessageSubscription> Subscribe(
            IAdapterCallContext context, 
            EventMessageSubscriptionType subscriptionType
        );

    }

}
