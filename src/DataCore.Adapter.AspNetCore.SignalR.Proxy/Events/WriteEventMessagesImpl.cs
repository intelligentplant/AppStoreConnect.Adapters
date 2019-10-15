using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IWriteEventMessages"/>.
    /// </summary>
    internal class WriteEventMessagesImpl : ProxyAdapterFeature, IWriteEventMessages {

        /// <summary>
        /// Creates a new <see cref="WriteEventMessagesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteEventMessagesImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<WriteEventMessageResult> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var hubChannel = await client.Events.WriteEventMessagesAsync(AdapterId, channel, ct).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }
    }
}
