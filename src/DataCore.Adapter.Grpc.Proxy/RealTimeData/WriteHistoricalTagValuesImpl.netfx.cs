﻿#if NETFRAMEWORK

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    partial class WriteHistoricalTagValuesImpl {

        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context,
            Adapter.RealTimeData.WriteTagValuesRequest request,
            IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var callOptions = GetCallOptions(context, ctSource.Token);

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

                    var grpcResponse = await client.WriteFixedHistoricalTagValuesAsync(grpcRequest, callOptions).ConfigureAwait(false);
                    foreach (var result in grpcResponse.Results) {
                        yield return result.ToAdapterWriteTagValueResult();
                    }
                }
            }
        }

    }
}
#endif