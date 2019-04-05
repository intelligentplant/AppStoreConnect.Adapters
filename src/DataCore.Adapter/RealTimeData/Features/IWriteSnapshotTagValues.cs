using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

    /// <summary>
    /// Feature for writing new snapshot values to tags.
    /// </summary>
    public interface IWriteSnapshotTagValues : IAdapterFeature {

        /// <summary>
        /// Writes values to the snapshot for the specified tags. Implementations should ignore any 
        /// values that are older than the current snapshot value for the tag.
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
