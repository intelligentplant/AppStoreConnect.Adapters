using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading tag values at specific time stamps from an adapter.
    /// </summary>
    /// <remarks>
    ///   The <see cref="Utilities.InterpolationHelper"/> class can assist with the calculation 
    ///   of values.
    /// </remarks>
    public interface IReadTagValuesAtTimes : IAdapterFeature {

        /// <summary>
        /// Reads values from the adapter with specific time stamps.
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
        ///   The values for the requested tags. The adapter can decide if it will interpolate a tag 
        ///   value using the closest raw samples to a requested time stamp, or if it will repeat the 
        ///   previous raw value before the time stamp.
        /// </returns>
        Task<IEnumerable<HistoricalTagValues>> ReadTagValuesAtTimes(IDataCoreContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken);


    }
}
