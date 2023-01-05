using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IWriteHistoricalTagValues"/>.
    /// </summary>
    internal class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteHistoricalTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagValues.WriteHistoricalTagValuesAsync(
                AdapterId,
                request,
                channel,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }
    }
}
