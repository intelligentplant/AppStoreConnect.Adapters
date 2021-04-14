using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for querying historical event messages using a cursor to represent the starting time 
    /// of the query.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Events.ReadEventMessagesUsingCursor,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadEventMessagesUsingCursor),
        Description = nameof(AbstractionsResources.Description_ReadEventMessagesUsingCursor)
    )]
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
        IAsyncEnumerable<EventMessageWithCursorPosition> ReadEventMessagesUsingCursor(
            IAdapterCallContext context, 
            ReadEventMessagesUsingCursorRequest request, 
            CancellationToken cancellationToken
        );

    }
}
