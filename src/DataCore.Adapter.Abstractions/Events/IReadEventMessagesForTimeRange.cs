using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature for querying historical event messages using a time range.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Events.ReadEventMessagesForTimeRange,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadEventMessagesForTimeRange),
        Description = nameof(AbstractionsResources.Description_ReadEventMessagesForTimeRange)
    )]
    public interface IReadEventMessagesForTimeRange : IAdapterFeature {

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
        IAsyncEnumerable<EventMessage> ReadEventMessagesForTimeRange(
            IAdapterCallContext context, 
            ReadEventMessagesForTimeRangeRequest request, 
            CancellationToken cancellationToken
        );

    }
}
