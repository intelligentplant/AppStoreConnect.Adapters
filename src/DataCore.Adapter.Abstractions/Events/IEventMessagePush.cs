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
        ///   A subscription object that can be disposed once the subscription is no longer required.
        /// </returns>
        IEventMessageSubscription Subscribe(
            IAdapterCallContext context, 
            EventMessageSubscriptionType subscriptionType
        );

    }

}
