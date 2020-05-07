using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Feature for requesting the health status of an adapter.
    /// </summary>
    public interface IHealthCheck : IAdapterFeature {

        /// <summary>
        /// Performs an adapter health check.
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
        Task<HealthCheckResult> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken);

    }
}
