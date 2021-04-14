using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> ReadRawTagValues(
            IAdapterCallContext context, 
            Adapter.RealTimeData.ReadRawTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

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

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcResponse = client.ReadRawTagValues(grpcRequest, GetCallOptions(context, ctSource.Token))) {
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
