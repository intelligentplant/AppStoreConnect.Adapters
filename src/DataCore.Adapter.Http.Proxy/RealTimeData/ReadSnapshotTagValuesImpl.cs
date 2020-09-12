using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {

    /// <summary>
    /// Implements <see cref="IReadSnapshotTagValues"/>.
    /// </summary>
    internal class ReadSnapshotTagValuesImpl : ProxyAdapterFeature, IReadSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadSnapshotTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadSnapshotTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagValues.ReadSnapshotTagValuesAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }

}
