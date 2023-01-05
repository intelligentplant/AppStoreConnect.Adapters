using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
        public async IAsyncEnumerable<Adapter.RealTimeData.DataFunctionDescriptor> GetSupportedDataFunctions(
            IAdapterCallContext context, 
            Adapter.RealTimeData.GetSupportedDataFunctionsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new GetSupportedDataFunctionsRequest() {
                AdapterId = AdapterId
            };

            if (request.Properties != null) {
                grpcRequest.Properties.Add(request.Properties);
            }

            using (var grpcResponse = client.GetSupportedDataFunctions(grpcRequest, GetCallOptions(context, cancellationToken))) {
                while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterDataFunctionDescriptor();
                }
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.RealTimeData.ProcessedTagValueQueryResult> ReadProcessedTagValues(
            IAdapterCallContext context, 
            Adapter.RealTimeData.ReadProcessedTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
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

            using (var grpcResponse = client.ReadProcessedTagValues(grpcRequest, GetCallOptions(context, cancellationToken))) {
                while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterProcessedTagValueQueryResult();
                }
            }
        }

    }
}
