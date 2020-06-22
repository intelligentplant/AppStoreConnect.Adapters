using System.Threading.Tasks;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Extends <see cref="IHealthCheck"/> to allow subscribers to receive health status changes 
    /// via push notifications.
    /// </summary>
    public interface IHealthCheckPush : IHealthCheck {

        /// <summary>
        /// Subscribes to receive adapter health check updates.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation, to allow authorization to be applied to the 
        ///   operation if required.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/ that will create and start an <see cref="IHealthCheckSubscription"/> 
        ///   that can be disposed once the subscription is no longer required.
        /// </returns>
        Task<IHealthCheckSubscription> Subscribe(IAdapterCallContext context);

    }
}
