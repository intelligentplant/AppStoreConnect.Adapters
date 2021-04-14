using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;
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
            Proxy.ValidateInvocation(context, request, channel);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var items = (await channel.ToEnumerable(1000, ctSource.Token).ConfigureAwait(false)).ToArray();

                var req = new WriteEventMessagesRequestExtended() {
                    Events = items,
                    Properties = request.Properties
                };

                var clientResponse = await client.Events.WriteEventMessagesAsync(AdapterId, req, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }
    }
}
