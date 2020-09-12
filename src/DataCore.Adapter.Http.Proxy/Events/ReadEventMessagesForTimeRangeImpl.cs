using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Http.Proxy.Events {
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
        public ReadEventMessagesForTimeRangeImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public Task<ChannelReader<EventMessage>> ReadEventMessagesForTimeRange(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateEventMessageChannel<EventMessage>(-1);

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
