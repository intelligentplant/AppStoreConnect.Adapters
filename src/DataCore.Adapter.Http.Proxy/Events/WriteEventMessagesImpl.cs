using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.Logging;

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
        public async IAsyncEnumerable<WriteEventMessageResult> WriteEventMessages(
            IAdapterCallContext context,
            WriteEventMessagesRequest request,
            IAsyncEnumerable<WriteEventMessageItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (Proxy.CanUseSignalR) {
                var client = GetSignalRClient(context);
                await client.StreamStartedAsync().ConfigureAwait(false);
                try {
                    await foreach (var item in client.Client.Events.WriteEventMessagesAsync(AdapterId, request, channel, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }
                        yield return item;
                    }
                }
                finally {
                    await client.StreamCompletedAsync().ConfigureAwait(false);
                }
            }
            else {
                var client = GetClient();

                var items = (await channel.ToEnumerable(1000, cancellationToken).ConfigureAwait(false)).ToArray();

                var req = new WriteEventMessagesRequestExtended() {
                    Events = items,
                    Properties = request.Properties
                };

                await foreach (var item in client.Events.WriteEventMessagesAsync(AdapterId, req, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }

            }
        }
    }
}
