﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> ReadPlotTagValues(
            IAdapterCallContext context, 
            Adapter.RealTimeData.ReadPlotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcRequest = new ReadPlotTagValuesRequest() {
                AdapterId = AdapterId,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime.ToUniversalTime()),
                UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime.ToUniversalTime()),
                Intervals = request.Intervals
            };
            grpcRequest.Tags.AddRange(request.Tags);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            using (var grpcResponse = client.ReadPlotTagValues(grpcRequest, GetCallOptions(context, cancellationToken))) {
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
