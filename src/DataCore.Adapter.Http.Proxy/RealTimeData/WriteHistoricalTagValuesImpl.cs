using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
    /// <summary>
    /// Implements <see cref="IWriteHistoricalTagValues"/>.
    /// </summary>
    internal partial class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteHistoricalTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (Proxy.CanUseSignalR) {
                var client = GetSignalRClient(context);
                await client.StreamStartedAsync().ConfigureAwait(false);
                try {
                    await foreach (var item in client.Client.TagValues.WriteHistoricalTagValuesAsync(AdapterId, request, channel, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }
                        yield return item;
                    }
                }
                finally {
                    await client.StreamCompletedAsync().ConfigureAwait(false);
                }
            }
            else {
                var client = GetClient();

                const int maxItems = 5000;
                var items = (await channel.ToEnumerable(maxItems, cancellationToken).ConfigureAwait(false)).ToArray();
                if (items.Length >= maxItems) {
                    LogMaxItemsReached(Logger, maxItems);
                }

                var req = new WriteTagValuesRequestExtended() {
                    Values = items,
                    Properties = request.Properties
                };

                await foreach (var item in client.TagValues.WriteHistoricalValuesAsync(AdapterId, req, context?.ToRequestMetadata(), cancellationToken).ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }

            }
        }


        [LoggerMessage(1, LogLevel.Information, "The maximum number of items that can be written to the remote adapter ({maxItems}) was read from the channel. Any remaining items will be ignored.")]
        static partial void LogMaxItemsReached(ILogger logger, int maxItems);

    }
}
