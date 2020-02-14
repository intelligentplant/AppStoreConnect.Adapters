using System.Linq;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
    /// <summary>
    /// Implements <see cref="IWriteSnapshotTagValues"/>.
    /// </summary>
    internal class WriteSnapshotTagValuesImpl : ProxyAdapterFeature, IWriteSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteSnapshotTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteSnapshotTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<WriteTagValueResult> WriteSnapshotTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();

                const int maxItems = 5000;
                var items = (await channel.ToEnumerable(maxItems, ct).ConfigureAwait(false)).ToArray();
                if (items.Length >= maxItems) {
                    Logger.LogInformation("The maximum number of items that can be written to the remote adapter ({MaxItems}) was read from the channel. Any remaining items will be ignored.", maxItems);
                }

                var request = new WriteTagValuesRequest() {
                    Values = items
                };

                var clientResponse = await client.TagValues.WriteSnapshotValuesAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
