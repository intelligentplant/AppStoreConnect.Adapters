using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Feature for requesting the health status of an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Diagnostics.HealthCheck,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_HealthCheck),
        Description = nameof(AbstractionsResources.Description_HealthCheck)
    )]
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
        ///   A <see cref="Task{TResult}"/> that will return the <see cref="HealthCheckResult"/> 
        ///   for the health check.
        /// </returns>
        Task<HealthCheckResult> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken);


        /// <summary>
        /// Subscribes to receive adapter health check updates.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation, to allow authorization to be applied to the 
        ///   operation if required.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the health check updates.
        /// </returns>
        IAsyncEnumerable<HealthCheckResult> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken);

    }
}
