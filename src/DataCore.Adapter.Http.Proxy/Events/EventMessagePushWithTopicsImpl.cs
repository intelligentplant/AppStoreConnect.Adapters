using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Http.Proxy.Events {

    /// <summary>
    /// Implements <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    internal class EventMessagePushWithTopicsImpl : ProxyAdapterFeature, IEventMessagePushWithTopics {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopicsImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public EventMessagePushWithTopicsImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetSignalRClient(context);
            await client.StreamStartedAsync().ConfigureAwait(false);

            try {
                await foreach (var item in client.Client.Events.CreateEventMessageTopicChannelAsync(AdapterId, request, channel, cancellationToken).ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }
            }
            finally {
                await client.StreamCompletedAsync().ConfigureAwait(false);
            }
        }
    }

}
