#if NETFRAMEWORK == false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using DataCore.Adapter.Events;


namespace DataCore.Adapter.Grpc.Proxy.Events {

    partial class EventMessagePushWithTopicsImpl {

        /// <inheritdoc/>
        public IAsyncEnumerable<Adapter.Events.EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            return SubscribeCoreAsync(context, request, channel, cancellationToken);
        }

    }

}

#endif
