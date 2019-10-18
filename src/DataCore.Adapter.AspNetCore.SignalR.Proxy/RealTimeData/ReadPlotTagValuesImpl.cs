using System.Threading;
using System.Threading.Channels;
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
        public ChannelReader<TagValueQueryResult> ReadPlotTagValues(IAdapterCallContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var hubChannel = await client.TagValues.ReadPlotTagValuesAsync(AdapterId, request, ct).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

    }
}
