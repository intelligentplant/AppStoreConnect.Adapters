using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for querying historical event messages using a cursor to represent the starting time 
    /// of the query.
    /// </summary>
    public interface IReadEventMessagesUsingCursor : IAdapterFeature {

        /// <summary>
        /// Reads historical event messages from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The event message query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The event messages that occurred during the time range.
        /// </returns>
        Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessages(
            IAdapterCallContext context, 
            ReadEventMessagesUsingCursorRequest request, 
            CancellationToken cancellationToken
        );

    }
}
