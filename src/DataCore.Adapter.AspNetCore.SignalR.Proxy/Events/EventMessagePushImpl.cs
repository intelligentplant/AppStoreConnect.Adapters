using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

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
        public EventMessagePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in GetClient().Events.CreateEventMessageChannelAsync(
                    AdapterId,
                    request,
                    ctSource.Token
                )) {
                    yield return item;
                }
            }
        }

    }
}
