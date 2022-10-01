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


        /// <inheritdoc/>
        public async IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.Events.CreateEventMessageSubscriptionAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }
            }
        }
    }

}
