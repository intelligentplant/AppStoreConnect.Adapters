using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

    /// <summary>
    /// Implements <see cref="IReadEventMessagesForTimeRange"/>.
    /// </summary>
    internal class ReadEventMessagesForTimeRangeImpl : ProxyAdapterFeature, IReadEventMessagesForTimeRange {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesForTimeRangeImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadEventMessagesForTimeRangeImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async Task<ChannelReader<EventMessage>> ReadEventMessagesForTimeRange(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            var hubChannel = await client.Events.ReadEventMessagesAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateEventMessageChannel<EventMessage>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
