using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading interpolated tag values from an adapter.
    /// </summary>
    /// <remarks>
    ///   The <see cref="Utilities.InterpolationHelper"/> class can assist with the calculation 
    ///   of values.
    /// </remarks>
    public interface IReadInterpolatedTagValues : IAdapterFeature {

        /// <summary>
        /// Reads interpolated data from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The data query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The interpolated values for the requested tags.
        /// </returns>
        Task<IEnumerable<HistoricalTagValues>> ReadInterpolatedTagValues(IDataCoreContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken);

    }
}
