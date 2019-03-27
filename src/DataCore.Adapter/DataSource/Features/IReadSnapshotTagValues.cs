using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading snapshot tag values from an adapter.
    /// </summary>
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
        ///   The snapshot values for the requested tags.
        /// </returns>
        Task<IEnumerable<SnapshotTagValue>> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken);

    }
}
