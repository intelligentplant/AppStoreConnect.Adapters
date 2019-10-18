using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IReadTagValuesAtTimes"/>.
    /// </summary>
    internal class ReadTagValuesAtTimesImpl : ProxyAdapterFeature, IReadTagValuesAtTimes {

        /// <summary>
        /// Creates a new <see cref="ReadTagValuesAtTimesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadTagValuesAtTimesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var hubChannel = await client.TagValues.ReadTagValuesAtTimesAsync(AdapterId, request, ct).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

    }
}
