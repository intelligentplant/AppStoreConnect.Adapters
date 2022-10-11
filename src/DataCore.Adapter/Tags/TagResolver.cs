using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Tags {
    /// <summary>
    /// A delegate that can resolve tags based on provided tag names or IDs.
    /// </summary>
    /// <param name="context">
    ///   The <see cref="IAdapterCallContext"/> for the caller.
    /// </param>
    /// <param name="namesOrIds">
    ///   The tag names or IDs to resolve.
    /// </param>
    /// <param name="cancellationToken">
    ///   The cancellation token for the operation.
    /// </param>
    /// <returns>
    ///   An <see cref="IAsyncEnumerable{T}"/> that will return the <see cref="TagIdentifier"/> 
    ///   instances for the items in <paramref name="namesOrIds"/> that could be resolved.
    /// </returns>
    public delegate IAsyncEnumerable<TagIdentifier> TagResolver(
        IAdapterCallContext context, 
        IEnumerable<string> namesOrIds, 
        CancellationToken cancellationToken
    );
}
