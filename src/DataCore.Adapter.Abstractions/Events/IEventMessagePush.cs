using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter via a push notification.
    /// </summary>
    [AdapterFeature(WellKnownFeatures.Events.EventMessagePush)]
    public interface IEventMessagePush : IAdapterFeature {

        /// <summary>
        /// Creates a push subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request describing the subscription settings.
        /// </param>
        /// <returns>
        ///   A task that will create and start a subscription object that can be disposed once 
        ///   the subscription is no longer required.
        /// </returns>
        Task<IEventMessageSubscription> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request
        );

    }

}
