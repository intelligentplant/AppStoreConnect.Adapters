using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for writing historical values to an adapter's data archive.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.WriteHistoricalTagValues,
        ResourceType = typeof(DataCoreAdapterAbstractionsResources),
        Name = nameof(DataCoreAdapterAbstractionsResources.DisplayName_WriteHistoricalTagValues),
        Description = nameof(DataCoreAdapterAbstractionsResources.Description_WriteHistoricalTagValues)
    )]
    public interface IWriteHistoricalTagValues : IAdapterFeature {

        /// <summary>
        /// Writes a stream of historical tag values to an adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="channel">
        ///   A <see cref="ChannelReader{T}"/> that will provide the tag values to write to the 
        ///   adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that will emit a write result for each item read from 
        ///   the input <paramref name="channel"/>.
        /// </returns>
        Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValues(
            IAdapterCallContext context, 
            ChannelReader<WriteTagValueItem> channel, 
            CancellationToken cancellationToken
        );

    }
}
