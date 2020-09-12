using System;
using System.Threading;
using System.Threading.Channels;
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
        public async Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = GetClient();
            var hubChannel = await client.TagValues.WriteHistoricalTagValuesAsync(
                AdapterId, 
                channel, 
                cancellationToken
            ).ConfigureAwait(false);
            
            var result = ChannelExtensions.CreateTagValueWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
