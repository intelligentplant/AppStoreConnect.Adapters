using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        public async Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = GetClient();
            var hubChannel = await client.Events.WriteEventMessagesAsync(
                AdapterId, 
                channel, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateEventMessageWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
