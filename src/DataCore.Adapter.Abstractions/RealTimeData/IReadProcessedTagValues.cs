using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading processed (aggregated) tag values from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadProcessedTagValues,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadProcessedTagValues),
        Description = nameof(AbstractionsResources.Description_ReadProcessedTagValues)
    )]
    public interface IReadProcessedTagValues : IAdapterFeature {

        /// <summary>
        /// Gets information about the data functions that can be specified when calling 
        /// <see cref="ReadProcessedTagValues"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit the available data functions.
        /// </returns>
        IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions(
            IAdapterCallContext context, 
            GetSupportedDataFunctionsRequest request,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Reads processed (aggregated) data from the adapter.
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
        IAsyncEnumerable<ProcessedTagValueQueryResult> ReadProcessedTagValues(
            IAdapterCallContext context, 
            ReadProcessedTagValuesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
