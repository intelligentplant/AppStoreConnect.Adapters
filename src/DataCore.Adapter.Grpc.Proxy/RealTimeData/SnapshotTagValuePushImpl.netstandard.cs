#if NETFRAMEWORK == false

using System.Collections.Generic;
using System.Threading;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    partial class SnapshotTagValuePushImpl {

        /// <inheritdoc />
        public IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            return SubscribeCoreAsync(context, request, channel, cancellationToken);
        }

    }

}

#endif
