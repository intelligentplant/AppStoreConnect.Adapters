#if NETFRAMEWORK == false

using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    partial class WriteHistoricalTagValuesImpl {

        /// <inheritdoc />
        public IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context,
            Adapter.RealTimeData.WriteTagValuesRequest request,
            IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueItem> channel,
            CancellationToken cancellationToken
        ) {
            return WriteHistoricalTagValuesCoreAsync(context, request, channel, cancellationToken);
        }

    }
}

#endif
