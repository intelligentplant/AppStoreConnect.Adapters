using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

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
        ///   The <see cref="IAdapterCallContext"/> for the caller.
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
        Task<IEnumerable<HistoricalTagValues>> ReadInterpolatedTagValues(IAdapterCallContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken);

    }
}
