using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a service for resolving adapters at runtime.
    /// </summary>
    public interface IAdapterAccessor {

        /// <summary>
        /// Gets the available adapters.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapters available to the caller.
        /// </returns>
        Task<IEnumerable<IAdapter>> GetAdapters(IDataCoreContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the specified adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The requested adapter.
        /// </returns>
        Task<IAdapter> GetAdapter(IDataCoreContext context, string adapterId, CancellationToken cancellationToken);

    }
}
