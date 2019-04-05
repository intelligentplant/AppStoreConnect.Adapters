using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

    /// <summary>
    /// Feature for writing historical values to an adapter's data archive.
    /// </summary>
    public interface IWriteHistoricalTagValues : IAdapterFeature {

        /// <summary>
        /// Writes values directly to the historian archive for the specified tags. Implementations 
        /// can choose if existing archive values with the same sample times should be kept or replaced.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The values to be written.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A collection of objects describing the write result for each tag in the request.
        /// </returns>
        Task<IEnumerable<TagValueWriteResult>> WriteSnapshotTagValues(IAdapterCallContext context, WriteTagValuesRequest request, CancellationToken cancellationToken);

    }
}
