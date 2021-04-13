using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for writing new snapshot values to adapter tags.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.WriteSnapshotTagValues,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_WriteSnapshotTagValues),
        Description = nameof(AbstractionsResources.Description_WriteSnapshotTagValues)
    )]
    public interface IWriteSnapshotTagValues : IAdapterFeature {

        /// <summary>
        /// Writes a stream of snapshot tag values to an adapter.
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
        IAsyncEnumerable<WriteTagValueResult> WriteSnapshotTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            CancellationToken cancellationToken
        );

    }
}
