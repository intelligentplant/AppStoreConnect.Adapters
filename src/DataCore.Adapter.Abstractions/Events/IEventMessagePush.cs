using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter via a push notification.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Events.EventMessagePush,
        ResourceType = typeof(DataCoreAdapterAbstractionsResources),
        Name = nameof(DataCoreAdapterAbstractionsResources.DisplayName_EventMessagePush),
        Description = nameof(DataCoreAdapterAbstractionsResources.Description_EventMessagePush)
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
        ///   A channel reader that will emit event messages as they occur.
        /// </returns>
        Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        );

    }

}
