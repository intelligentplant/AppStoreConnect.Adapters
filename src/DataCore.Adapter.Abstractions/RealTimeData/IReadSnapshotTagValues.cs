using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading snapshot tag values from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadSnapshotTagValues,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadSnapshotTagValues),
        Description = nameof(AbstractionsResources.Description_ReadSnapshotTagValues)
    )]
    public interface IReadSnapshotTagValues : IAdapterFeature {

        /// <summary>
        /// Reads snapshot data from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The data query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will complete once the request has completed.
        /// </returns>
        Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(
            IAdapterCallContext context, 
            ReadSnapshotTagValuesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
