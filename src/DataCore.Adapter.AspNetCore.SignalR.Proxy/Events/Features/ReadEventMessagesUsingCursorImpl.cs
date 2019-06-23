using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {
    internal class ReadEventMessagesUsingCursorImpl : ProxyAdapterFeature, IReadEventMessagesUsingCursor {

        public ReadEventMessagesUsingCursorImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        public ChannelReader<EventMessageWithCursorPosition> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<EventMessageWithCursorPosition>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await this.GetTagValuesHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<EventMessageWithCursorPosition>(
                    "ReadEventMessagesUsingCursor",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }
    }
}
