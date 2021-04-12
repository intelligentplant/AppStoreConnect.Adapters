using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for writing historical values to an adapter's data archive.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.WriteHistoricalTagValues,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_WriteHistoricalTagValues),
        Description = nameof(AbstractionsResources.Description_WriteHistoricalTagValues)
    )]
    public interface IWriteHistoricalTagValues : IAdapterFeature {

        /// <summary>
        /// Writes a stream of historical tag values to an adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="channel">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the tag values to write 
        ///   to the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit a write result for each item 
        ///   read from the input <paramref name="channel"/>.
        /// </returns>
        IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            CancellationToken cancellationToken
        );

    }
}
