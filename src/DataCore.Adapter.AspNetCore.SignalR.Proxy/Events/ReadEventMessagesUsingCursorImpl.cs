using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

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
        public ReadEventMessagesUsingCursorImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessagesUsingCursor(IAdapterCallContext context, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            var hubChannel = await client.Events.ReadEventMessagesAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);

            var result = ChannelExtensions.CreateEventMessageChannel<EventMessageWithCursorPosition>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, BackgroundTaskService, cancellationToken);

            return result;
        }
    }
}
