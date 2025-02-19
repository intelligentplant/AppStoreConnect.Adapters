using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="FeatureBase"/> is a base class that adapter features implemented externally to 
    /// an adapter can inherit from. <see cref="FeatureBase"/> provides a built-in implementation 
    /// of <see cref="IFeatureHealthCheck"/> that implementers can extend to provide custom health 
    /// checks fr the feature.
    /// </summary>
    public abstract class FeatureBase : IFeatureHealthCheck {

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var name = GetFeatureHealthCheckName();
            var innerResults = await CheckFeatureHealthCoreAsync(context, cancellationToken).ConfigureAwait(false);
            Dictionary<string, string>? data = null;

            foreach (var item in GetFeatureHealthCheckData(context)) {
                data ??= new Dictionary<string, string>();
                data[item.Key] = item.Value;
            }

            var result = innerResults?.Any() ?? false
                ? HealthCheckResult.Composite(name, innerResults, data: data)
                : HealthCheckResult.Healthy(name, data: data);

            return result;
        }


        /// <summary>
        /// Performs custom feature health checks.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A collection of custom health check results.
        /// </returns>
        protected virtual Task<IEnumerable<HealthCheckResult>> CheckFeatureHealthCoreAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            return Task.FromResult(Enumerable.Empty<HealthCheckResult>());
        }


        /// <summary>
        /// Gets the name to use for the root feature health check.
        /// </summary>
        /// <returns>
        ///   The health check name.
        /// </returns>
        protected virtual string GetFeatureHealthCheckName() => GetType().Name;


        /// <summary>
        /// Gets custom data properties to include in the root feature health check result 
        /// returned by <see cref="CheckFeatureHealthAsync"/>.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation.
        /// </param>
        /// <returns>
        ///   A collection of key-value pairs representing the custom data properties.
        /// </returns>
        protected virtual IEnumerable<KeyValuePair<string, string>> GetFeatureHealthCheckData(IAdapterCallContext context) {
            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

    }
}
