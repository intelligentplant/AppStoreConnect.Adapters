using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading processed (aggregated) tag values from an adapter.
    /// </summary>
    /// <remarks>
    ///   The <see cref="Utilities.AggregationHelper"/> class can assist with the calculation 
    ///   of values.
    /// </remarks>
    public interface IReadProcessedTagValues : IAdapterFeature {

        Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken);

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
        ///   The values for the requested tags and aggregate functions.
        /// </returns>
        Task<IEnumerable<ProcessedHistoricalTagValues>> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken);

    }
}
