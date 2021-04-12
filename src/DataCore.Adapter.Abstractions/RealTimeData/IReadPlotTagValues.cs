using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading visualization-friendly tag values from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadPlotTagValues,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadPlotTagVaues),
        Description = nameof(AbstractionsResources.Description_ReadPlotTagVaues)
    )]
    public interface IReadPlotTagValues : IAdapterFeature {

        /// <summary>
        /// Reads plot data from the adapter.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the requested tag values.
        /// </returns>
        IAsyncEnumerable<TagValueQueryResult> ReadPlotTagValues(
            IAdapterCallContext context, 
            ReadPlotTagValuesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
