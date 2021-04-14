using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IWriteHistoricalTagValues"/> implementation.
    /// </summary>
    internal class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public WriteHistoricalTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<Adapter.RealTimeData.WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcStream = client.WriteHistoricalTagValues(GetCallOptions(context, ctSource.Token))) {
                // Create the subscription.
                var initMessage = new WriteTagValueInitMessage() {
                    AdapterId = AdapterId
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
                }, ctSource.Token);

                while (await grpcStream.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    yield return grpcStream.ResponseStream.Current.ToAdapterWriteTagValueResult();
                }
            }
        }

    }

}
