#if NETFRAMEWORK

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    partial class WriteSnapshotTagValuesImpl {

        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueResult> WriteSnapshotTagValues(
            IAdapterCallContext context,
            Adapter.RealTimeData.WriteTagValuesRequest request,
            IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (GrpcAdapterProxy.IsGrpcClientFullySupported()) {
                // Bidirectional streaming is fully supported.
                await foreach (var item in WriteSnapshotTagValuesCoreAsync(context, request, channel, cancellationToken).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }

            // Bidirectional streaming is not supported.

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();

            var callOptions = GetCallOptions(context, cancellationToken);

            await foreach (var item in channel.ConfigureAwait(false)) {
                var grpcRequest = new WriteTagValuesRequest() {
                    AdapterId = AdapterId
                };

                if (request.Properties != null) {
                    foreach (var prop in request.Properties) {
                        grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                    }
                }

                grpcRequest.Values.Add(item.ToGrpcWriteTagValueItem());

                var grpcResponse = await client.WriteFixedSnapshotTagValuesAsync(grpcRequest, callOptions).ConfigureAwait(false);
                foreach (var result in grpcResponse.Results) {
                    yield return result.ToAdapterWriteTagValueResult();
                }
            }
        }

    }
}
#endif
