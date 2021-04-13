using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> ReadTagValuesAtTimes(
            IAdapterCallContext context, 
            Adapter.RealTimeData.ReadTagValuesAtTimesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

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

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcResponse = client.ReadTagValuesAtTimes(grpcRequest, GetCallOptions(context, ctSource.Token))) {
                while (await grpcResponse.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterTagValueQueryResult();
                }
            }
        }

    }

}
