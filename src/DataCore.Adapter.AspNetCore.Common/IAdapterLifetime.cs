using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// A service for performing actions triggered by adapter lifetime events.
    /// </summary>
    public interface IAdapterLifetime {

        /// <summary>
        /// Performs actions when an adapter is started.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will perform post-init actions.
        /// </returns>
        Task StartedAsync(IAdapter adapter, CancellationToken cancellationToken);

        /// <summary>
        /// Performs actions when an adapter is stopped.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will perform post-init actions.
        /// </returns>
        Task StoppedAsync(IAdapter adapter, CancellationToken cancellationToken);

    }
}
