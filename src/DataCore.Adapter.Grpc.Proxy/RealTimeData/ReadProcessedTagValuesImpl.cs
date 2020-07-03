using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IReadProcessedTagValues"/> implementation.
    /// </summary>
    internal class ReadProcessedTagValuesImpl : ProxyAdapterFeature, IReadProcessedTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadProcessedTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadProcessedTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.RealTimeData.DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new GetSupportedDataFunctionsRequest() {
                AdapterId = AdapterId
            };

            var grpcResponse = client.GetSupportedDataFunctions(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateChannel<Adapter.RealTimeData.DataFunctionDescriptor>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterDataFunctionDescriptor(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.RealTimeData.ProcessedTagValueQueryResult>> ReadProcessedTagValues(IAdapterCallContext context, Adapter.RealTimeData.ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new ReadProcessedTagValuesRequest() {
                AdapterId = AdapterId,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                SampleInterval = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(request.SampleInterval)
            };
            grpcRequest.Tags.AddRange(request.Tags);
            grpcRequest.DataFunctions.AddRange(request.DataFunctions);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.ReadProcessedTagValues(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.ProcessedTagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterProcessedTagValueQueryResult(), ct).ConfigureAwait(false);
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
