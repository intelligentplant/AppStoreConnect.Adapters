using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IReadPlotTagValues"/> implementation.
    /// </summary>
    internal class ReadPlotTagValuesImpl : ProxyAdapterFeature, IReadPlotTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadPlotTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadPlotTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public ChannelReader<Adapter.RealTimeData.TagValueQueryResult> ReadPlotTagValues(IAdapterCallContext context, Adapter.RealTimeData.ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcRequest = new ReadPlotTagValuesRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                    Intervals = request.Intervals
                };
                grpcRequest.Tags.AddRange(request.Tags);

                var grpcResponse = client.ReadPlotTagValues(grpcRequest, GetCallOptions(context, ct));
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagValueQueryResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
