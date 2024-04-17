#if NETFRAMEWORK == false

using IntelligentPlant.BackgroundTasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    partial class WriteEventMessagesImpl {

        /// <inheritdoc/>
        public IAsyncEnumerable<Adapter.Events.WriteEventMessageResult> WriteEventMessages(
            IAdapterCallContext context,
            Adapter.Events.WriteEventMessagesRequest request,
            IAsyncEnumerable<Adapter.Events.WriteEventMessageItem> channel,
            CancellationToken cancellationToken
        ) {
            return WriteEventMessagesCoreAsync(context, request, channel, cancellationToken);
        }

    }
}

#endif
