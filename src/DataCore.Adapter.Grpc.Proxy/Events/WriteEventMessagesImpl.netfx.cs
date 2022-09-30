#if NETFRAMEWORK

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    partial class WriteEventMessagesImpl {

        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.Events.WriteEventMessageResult> WriteEventMessages(
            IAdapterCallContext context,
            Adapter.Events.WriteEventMessagesRequest request,
            IAsyncEnumerable<Adapter.Events.WriteEventMessageItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = CreateClient<EventsService.EventsServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var callOptions = GetCallOptions(context, ctSource.Token);

                await foreach (var item in channel.ConfigureAwait(false)) {
                    var grpcRequest = new WriteEventMessagesRequest() { 
                        AdapterId = AdapterId
                    };

                    if (request.Properties != null) {
                        foreach (var prop in request.Properties) {
                            grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                        }
                    }

                    grpcRequest.Messages.Add(item.ToGrpcWriteEventMessageItem());

                    var grpcResponse = await client.WriteFixedEventMessagesAsync(grpcRequest, callOptions).ConfigureAwait(false);
                    foreach (var result in grpcResponse.Results) {
                        yield return result.ToAdapterWriteEventMessageResult();
                    }
                }
            }
        }

    }
}
#endif
