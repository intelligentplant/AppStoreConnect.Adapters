using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {

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
        public ReadTagValuesAtTimesImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagValues.ReadTagValuesAtTimesAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
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
