#if NETFRAMEWORK == false

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

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
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();

            using (var grpcStream = client.WriteSnapshotTagValues(GetCallOptions(context, cancellationToken))) {
                // Create the subscription.
                var initMessage = new WriteTagValueInitMessage() {
                    AdapterId = Proxy.RemoteDescriptor.Id
                };

                if (request.Properties != null) {
                    foreach (var prop in request.Properties) {
                        initMessage.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                    }
                }

                await grpcStream.RequestStream.WriteAsync(new WriteTagValueRequest() {
                    Init = initMessage
                }).ConfigureAwait(false);

                // Run a background task to stream the values to write.
                Proxy.BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    try {
                        await foreach (var item in channel.WithCancellation(ct).ConfigureAwait(false)) {
                            await grpcStream.RequestStream.WriteAsync(new WriteTagValueRequest() {
                                Write = item.ToGrpcWriteTagValueItem()
                            }).ConfigureAwait(false);
                        }
                    }
                    finally {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }, cancellationToken);

                while (await grpcStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    yield return grpcStream.ResponseStream.Current.ToAdapterWriteTagValueResult();
                }
            }
        }

    }
}

#endif
