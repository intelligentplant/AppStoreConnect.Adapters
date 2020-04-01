using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Implement <see cref="IFeatureHealthCheck"/> on a delegated adapter feature to include it 
    /// in the health status of any adapter inheriting from <see cref="AdapterBase"/> or 
    /// <see cref="AdapterBase{TAdapterOptions}"/>.
    /// </summary>
    public interface IFeatureHealthCheck {

        /// <summary>
        /// Performs an adapter feature health check.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation, to allow authorization to be applied to the 
        ///   operation if required.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will return the <see cref="HealthCheckResult"/> for the 
        ///   health check.
        /// </returns>
        Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken);

    }
}
