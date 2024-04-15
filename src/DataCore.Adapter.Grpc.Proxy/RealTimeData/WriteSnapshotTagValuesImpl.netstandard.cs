#if NETFRAMEWORK == false

using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    partial class WriteSnapshotTagValuesImpl {

        /// <inheritdoc />
        public IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueResult> WriteSnapshotTagValues(
            IAdapterCallContext context,
            Adapter.RealTimeData.WriteTagValuesRequest request,
            IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueItem> channel,
            CancellationToken cancellationToken
        ) {
            return WriteSnapshotTagValuesCoreAsync(context, request, channel, cancellationToken);
        }

    }
}

#endif
