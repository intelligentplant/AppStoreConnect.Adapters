using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IReadSnapshotTagValues"/> implementation.
    /// </summary>
    internal class ReadSnapshotTagValuesImpl : ProxyAdapterFeature, IReadSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadSnapshotTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadSnapshotTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context,
            Adapter.RealTimeData.ReadSnapshotTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new ReadSnapshotTagValuesRequest() {
                AdapterId = AdapterId
            };
            grpcRequest.Tags.AddRange(request.Tags);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            using (var grpcResponse = client.ReadSnapshotTagValues(grpcRequest, GetCallOptions(context, cancellationToken))) {
                while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterTagValueQueryResult();
                }
            }
        }

    }

}
