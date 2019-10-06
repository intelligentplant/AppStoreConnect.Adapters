using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;

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
        public ChannelReader<EventMessageWithCursorPosition> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<EventMessageWithCursorPosition>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.Events.ReadEventMessagesAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, cancellationToken);

            return result;
        }
    }
}
