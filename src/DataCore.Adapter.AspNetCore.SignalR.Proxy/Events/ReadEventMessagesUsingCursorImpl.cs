using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

    /// <summary>
    /// Implements <see cref="IReadEventMessagesUsingCursor"/>.
    /// </summary>
    internal class ReadEventMessagesUsingCursorImpl : ProxyAdapterFeature, IReadEventMessagesUsingCursor {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesUsingCursorImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadEventMessagesUsingCursorImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<EventMessageWithCursorPosition> ReadEventMessagesUsingCursor(
            IAdapterCallContext context, 
            ReadEventMessagesUsingCursorRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            
            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach(var item in client.Events.ReadEventMessagesAsync(AdapterId, request, ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }

        }
    }
}
