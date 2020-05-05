using System.Threading;
using System.Threading.Channels;
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
        public ChannelReader<Adapter.Events.WriteEventMessageResult> WriteEventMessages(IAdapterCallContext context, ChannelReader<Adapter.Events.WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();
                var grpcStream = client.WriteEventMessages(GetCallOptions(context, ct));

                channel.RunBackgroundOperation(async (ch2, ct2) => {
                    try {
                        while (await ch2.WaitToReadAsync(ct2).ConfigureAwait(false)) {
                            if (ch2.TryRead(out var item) && item != null) {
                                await grpcStream.RequestStream.WriteAsync(item.ToGrpcWriteEventMessageItem(AdapterId)).ConfigureAwait(false);
                            }
                        }
                    }
                    finally {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }, TaskScheduler, ct);

                try {
                    while (await grpcStream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcStream.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteEventMessageResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcStream.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
