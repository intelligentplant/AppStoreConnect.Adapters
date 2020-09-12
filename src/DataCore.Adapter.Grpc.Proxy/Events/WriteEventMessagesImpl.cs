using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    /// <summary>
    /// <see cref="IWriteEventMessages"/> implementation.
    /// </summary>
    internal class WriteEventMessagesImpl : ProxyAdapterFeature, IWriteEventMessages {

        /// <summary>
        /// Creates a new <see cref="WriteEventMessagesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public WriteEventMessagesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<ChannelReader<Adapter.Events.WriteEventMessageResult>> WriteEventMessages(IAdapterCallContext context, ChannelReader<Adapter.Events.WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = CreateClient<EventsService.EventsServiceClient>();
            var grpcStream = client.WriteEventMessages(GetCallOptions(context, cancellationToken));

            // Create the subscription.
            await grpcStream.RequestStream.WriteAsync(new WriteEventMessageRequest() { 
                Init = new WriteEventMessageInitMessage() {
                    AdapterId = AdapterId
                }
            }).ConfigureAwait(false);

            // Stream subscription changes.
            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        while (ch.TryRead(out var update)) {
                            if (update == null) {
                                continue;
                            }

                            await grpcStream.RequestStream.WriteAsync(new WriteEventMessageRequest() {
                                Write = update.ToGrpcWriteEventMessageItem()
                            }).ConfigureAwait(false);
                        }
                    }
                }
                finally {
                    if (!ct.IsCancellationRequested) {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }
            }, BackgroundTaskService, cancellationToken);

            // Stream the results.
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                // Read results.
                while (await grpcStream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                    if (grpcStream.ResponseStream.Current == null) {
                        continue;
                    }

                    await result.Writer.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteEventMessageResult(), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }
    }
}
