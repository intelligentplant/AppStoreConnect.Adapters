using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {
    internal class WriteEventMessagesImpl : ProxyAdapterFeature, IWriteEventMessages {

        public WriteEventMessagesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        public ChannelReader<WriteEventMessageResult> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await GetHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<WriteEventMessageResult>(
                    "WriteEventMessages",
                    AdapterId,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }
    }
}
