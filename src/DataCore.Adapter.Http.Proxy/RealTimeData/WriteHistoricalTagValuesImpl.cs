using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
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
        public WriteHistoricalTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<WriteTagValueResult> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();

                var items = await channel.ReadItems(5000, ct).ConfigureAwait(false);
                var request = new WriteTagValuesRequest() {
                    Values = items.ToArray()
                };

                var clientResponse = await client.TagValues.WriteHistoricalValuesAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, cancellationToken);

            return result;
        }
    }
}
