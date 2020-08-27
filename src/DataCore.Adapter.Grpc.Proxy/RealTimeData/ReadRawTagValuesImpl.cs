using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IReadRawTagValues"/> implementation.
    /// </summary>
    internal class ReadRawTagValuesImpl : ProxyAdapterFeature, IReadRawTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadRawTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadRawTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.RealTimeData.TagValueQueryResult>> ReadRawTagValues(IAdapterCallContext context, Adapter.RealTimeData.ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            GrpcAdapterProxy.ValidateObject(request);

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new ReadRawTagValuesRequest() {
                AdapterId = AdapterId,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                SampleCount = request.SampleCount,
                BoundaryType = request.BoundaryType.ToGrpcRawDataBoundaryType()
            };
            grpcRequest.Tags.AddRange(request.Tags);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.ReadRawTagValues(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
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

            return Task.FromResult(result.Reader);
        }
    }
}
