using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Http.Proxy.Events {

    /// <summary>
    /// Implements <see cref="IEventMessagePush"/>.
    /// </summary>
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public EventMessagePushImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetSignalRClient(context);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await client.StreamStartedAsync().ConfigureAwait(false);

                try {
                    await foreach (var item in client.Client.Events.CreateEventMessageChannelAsync(AdapterId, request, ctSource.Token).ConfigureAwait(false)) {
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

}
