using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Wrapper for <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    internal class EventMessagePushWithTopicsWrapper : AdapterFeatureWrapper<IEventMessagePushWithTopics>, IEventMessagePushWithTopics {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopicsWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal EventMessagePushWithTopicsWrapper(AdapterCore adapter, IEventMessagePushWithTopics innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<EventMessage> IEventMessagePushWithTopics.Subscribe(IAdapterCallContext context, CreateEventMessageTopicSubscriptionRequest request, IAsyncEnumerable<EventMessageSubscriptionUpdate> subscriptionUpdates, CancellationToken cancellationToken) {
            return DuplexStreamAsync(context, request, subscriptionUpdates, InnerFeature.Subscribe, cancellationToken);
        }

    }

}
