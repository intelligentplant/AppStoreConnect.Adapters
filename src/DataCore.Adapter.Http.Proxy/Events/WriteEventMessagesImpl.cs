using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Http.Proxy.Events {
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
        public WriteEventMessagesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public ChannelReader<WriteEventMessageResult> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();

                var items = await channel.ReadItems(1000, ct).ConfigureAwait(false);
                var request = new WriteEventMessagesRequest() { 
                    Events = items.ToArray()
                };

                var clientResponse = await client.Events.WriteEventMessagesAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
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
