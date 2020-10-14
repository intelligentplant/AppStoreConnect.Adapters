using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

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
        public async Task<ChannelReader<Adapter.RealTimeData.WriteTagValueResult>> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<Adapter.RealTimeData.WriteTagValueItem> channel, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcStream = client.WriteHistoricalTagValues(GetCallOptions(context, cancellationToken));

            // Create the subscription.
            await grpcStream.RequestStream.WriteAsync(new WriteTagValueRequest() {
                Init = new WriteTagValueInitMessage() {
                    AdapterId = AdapterId
                }
            }).ConfigureAwait(false);

            // Flag is set to true when the input channel completes.
            var complete = false;
            // Tracks the number of responses that are pending from the gRPC server.
            var pendingResponseCount = 0;

            // Stream subscription changes.
            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        while (ch.TryRead(out var update)) {
                            if (update == null) {
                                continue;
                            }

                            await grpcStream.RequestStream.WriteAsync(new WriteTagValueRequest() {
                                Write = update.ToGrpcWriteTagValueItem()
                            }).ConfigureAwait(false);

                            Interlocked.Increment(ref pendingResponseCount);
                        }
                    }
                }
                finally {
                    // We do not call CompleteAsync on the gRPC request stream here, because doing 
                    // so will also cause the response stream to complete, and we may still be 
                    // waiting for pending write results. Instead, we will set a flag that the 
                    // response stream task will monitor to determine if it needs to keep running.
                    complete = true;
                }
            }, BackgroundTaskService, cancellationToken);

            // Stream the results.
            var result = ChannelExtensions.CreateTagValueWriteResultChannel(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    // Read results as long as the request stream has not completed or we are 
                    // still expecting results from the response stream.
                    while (!complete || pendingResponseCount > 0) {
                        if (!await grpcStream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                            break;
                        }

                        Interlocked.Decrement(ref pendingResponseCount);

                        if (grpcStream.ResponseStream.Current == null) {
                            continue;
                        }
                        await result.Writer.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteTagValueResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    if (!ct.IsCancellationRequested) {
                        // Notify the gRPC server that no more request items are coming.
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }

}
