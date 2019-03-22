using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for performing tag searches on an adapter.
    /// </summary>
    public interface ITagSearch : IAdapterFeature {

        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tag definitions.
        /// </returns>
        Task<IEnumerable<TagDefinition>> FindTags(IDataCoreContext context, FindTagsRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets tags by ID or name.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tag definitions.
        /// </returns>
        Task<IEnumerable<TagDefinition>> GetTags(IDataCoreContext context, GetTagsRequest request, CancellationToken cancellationToken);

    }
}
