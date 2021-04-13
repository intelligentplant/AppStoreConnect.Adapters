using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
                await foreach (var item in client.Events.WriteEventMessagesAsync(
                    AdapterId,
                    request,
                    channel,
                    ctSource.Token
                ).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }
    }
}
