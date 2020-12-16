using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IReadTagValuesAtTimes"/> implementation.
    /// </summary>
    internal class ReadTagValuesAtTimesImpl : ProxyAdapterFeature, IReadTagValuesAtTimes {

        /// <summary>
        /// Creates a new <see cref="ReadTagValuesAtTimesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadTagValuesAtTimesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.RealTimeData.TagValueQueryResult>> ReadTagValuesAtTimes(IAdapterCallContext context, Adapter.RealTimeData.ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            GrpcAdapterProxy.ValidateObject(request);

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new ReadTagValuesAtTimesRequest() {
                AdapterId = AdapterId
            };
            grpcRequest.Tags.AddRange(request.Tags);
            grpcRequest.UtcSampleTimes.AddRange(request.UtcSampleTimes.Select(x => Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(x)));
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.ReadTagValuesAtTimes(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateTagValueChannel(-1);

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
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }

}
