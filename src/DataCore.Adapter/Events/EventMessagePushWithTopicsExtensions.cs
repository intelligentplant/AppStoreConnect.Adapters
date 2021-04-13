using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Extensions for <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    public static class EventMessagePushWithTopicsExtensions {

        /// <summary>
        /// Creates a topic-based event message subscription that cannot be modified after creation.
        /// </summary>
        /// <param name="feature">
        ///   The feature.
        /// </param>
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
        public static IAsyncEnumerable<EventMessage> Subscribe(
            this IEventMessagePushWithTopics feature,
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            var channel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();
            channel.Writer.TryComplete();

            return feature.Subscribe(context, request, channel.Reader.ReadAllAsync(cancellationToken), cancellationToken);
        }

    }
}
