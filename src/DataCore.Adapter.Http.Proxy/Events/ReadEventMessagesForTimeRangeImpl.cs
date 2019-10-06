﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;

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
        public ChannelReader<EventMessage> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<EventMessage>(-1);

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
