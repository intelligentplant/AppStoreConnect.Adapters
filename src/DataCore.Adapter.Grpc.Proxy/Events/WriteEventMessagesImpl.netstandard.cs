#if NETFRAMEWORK == false

using IntelligentPlant.BackgroundTasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    partial class WriteEventMessagesImpl {

        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.Events.WriteEventMessageResult> WriteEventMessages(
            IAdapterCallContext context,
            Adapter.Events.WriteEventMessagesRequest request,
            IAsyncEnumerable<Adapter.Events.WriteEventMessageItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = CreateClient<EventsService.EventsServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcStream = client.WriteEventMessages(GetCallOptions(context, ctSource.Token))) {

                // Create the subscription.
                var initMessage = new WriteEventMessageInitMessage() {
                    AdapterId = AdapterId
                };

                if (request.Properties != null) {
                    foreach (var prop in request.Properties) {
                        initMessage.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                    }
                }

                await grpcStream.RequestStream.WriteAsync(new WriteEventMessageRequest() {
                    Init = initMessage
                }).ConfigureAwait(false);

                // Run a background task to stream the values to write.
                Proxy.BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    try {
                        await foreach (var item in channel.WithCancellation(ct).ConfigureAwait(false)) {
                            await grpcStream.RequestStream.WriteAsync(new WriteEventMessageRequest() {
                                Write = item.ToGrpcWriteEventMessageItem()
                            }).ConfigureAwait(false);
                        }
                    }
                    finally {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }, ctSource.Token);

                while (await grpcStream.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    yield return grpcStream.ResponseStream.Current.ToAdapterWriteEventMessageResult();
                }
            }
        }

    }
}

#endif
