using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IReadRawTagValues"/>.
    /// </summary>
    internal class ReadRawTagValuesImpl : ProxyAdapterFeature, IReadRawTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadRawTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadRawTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<TagValueQueryResult> ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var hubChannel = await client.TagValues.ReadRawTagValuesAsync(AdapterId, request, ct).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

    }
}
