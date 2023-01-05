using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IReadPlotTagValues"/>.
    /// </summary>
    internal class ReadPlotTagValuesImpl : ProxyAdapterFeature, IReadPlotTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadPlotTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadPlotTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagValueQueryResult> ReadPlotTagValues(
            IAdapterCallContext context, 
            ReadPlotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagValues.ReadPlotTagValuesAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }

    }
}
