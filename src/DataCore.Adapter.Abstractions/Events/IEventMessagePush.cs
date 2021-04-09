using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter via a push notification.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Events.EventMessagePush,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_EventMessagePush),
        Description = nameof(AbstractionsResources.Description_EventMessagePush)
    )]
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit event messages as they occur.
        /// </returns>
        IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        );

    }

}
