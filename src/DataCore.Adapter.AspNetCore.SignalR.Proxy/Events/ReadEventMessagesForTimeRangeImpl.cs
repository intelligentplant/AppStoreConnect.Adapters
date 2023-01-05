using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

    /// <summary>
    /// Implements <see cref="IReadEventMessagesForTimeRange"/>.
    /// </summary>
    internal class ReadEventMessagesForTimeRangeImpl : ProxyAdapterFeature, IReadEventMessagesForTimeRange {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesForTimeRangeImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadEventMessagesForTimeRangeImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<EventMessage> ReadEventMessagesForTimeRange(
            IAdapterCallContext context, 
            ReadEventMessagesForTimeRangeRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();

            await foreach (var item in client.Events.ReadEventMessagesAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }
    }
}
