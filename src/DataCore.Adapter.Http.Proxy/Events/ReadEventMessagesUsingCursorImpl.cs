using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Http.Proxy.Events {
    /// <summary>
    /// Implements <see cref="IReadEventMessagesUsingCursor"/>.
    /// </summary>
    internal class ReadEventMessagesUsingCursorImpl : ProxyAdapterFeature, IReadEventMessagesUsingCursor {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesUsingCursorImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadEventMessagesUsingCursorImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<EventMessageWithCursorPosition>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.Events.ReadEventMessagesAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }
}
