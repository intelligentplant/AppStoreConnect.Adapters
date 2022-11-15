using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Wrapper for <see cref="IEventMessagePush"/>.
    /// </summary>
    internal class EventMessagePushWrapper : AdapterFeatureWrapper<IEventMessagePush>, IEventMessagePush {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal EventMessagePushWrapper(AdapterCore adapter, IEventMessagePush innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<EventMessage> IEventMessagePush.Subscribe(IAdapterCallContext context, CreateEventMessageSubscriptionRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.Subscribe, cancellationToken);
        }

    }

}
