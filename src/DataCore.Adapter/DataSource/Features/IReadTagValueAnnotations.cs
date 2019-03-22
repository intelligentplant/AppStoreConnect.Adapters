using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading tag value annotations from an adapter.
    /// </summary>
    public interface IReadTagValueAnnotations : IAdapterFeature {

        /// <summary>
        /// Reads annotations from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The annotation query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The annotations for the requested tags.
        /// </returns>
        Task<IEnumerable<TagValueAnnotations>> ReadTagValueAnnotations(IDataCoreContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken);

    }
}
