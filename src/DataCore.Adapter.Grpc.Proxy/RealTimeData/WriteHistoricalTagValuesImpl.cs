using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IWriteHistoricalTagValues"/> implementation.
    /// </summary>
    internal class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public WriteHistoricalTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public Task<ChannelReader<Adapter.RealTimeData.WriteTagValueResult>> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcStream = client.WriteHistoricalTagValues(GetCallOptions(context, cancellationToken));

            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (ch.TryRead(out var item) && item != null) {
                            await grpcStream.RequestStream.WriteAsync(item.ToGrpcWriteTagValueItem(AdapterId)).ConfigureAwait(false);
                        }
                    }
                }
                finally {
                    await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                }
            }, TaskScheduler, cancellationToken);

            var result = ChannelExtensions.CreateTagValueWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await grpcStream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcStream.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteTagValueResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcStream.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }

}
