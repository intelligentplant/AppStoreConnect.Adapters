using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DataCore.Adapter.AspNetCore.Diagnostics.HealthChecks {

    /// <summary>
    /// <see cref="IHealthCheck"/> that checks the health of all adapters hosted by the 
    /// application.
    /// </summary>
    public sealed class AdapterHealthCheck : IHealthCheck {

        /// <summary>
        /// The <see cref="IAdapterAccessor"/> to use to retrieve the adapters to monitor.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AdapterHealthCheck"/>.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/> to use to retrieve the adapters to monitor.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterAccessor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterHealthCheck(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
            var adapterCallContext = new DefaultAdapterCallContext();
            var adapters = _adapterAccessor.GetAllAdapters(adapterCallContext, cancellationToken);

            var healthChecks = new Dictionary<string, Adapter.Diagnostics.HealthCheckResult>(StringComparer.OrdinalIgnoreCase);

            await foreach (var item in adapters.ConfigureAwait(false)) {
                try {
                    var feature = item.GetFeature<Adapter.Diagnostics.IHealthCheck>(WellKnownFeatures.Diagnostics.HealthCheck);
                    if (feature == null) {
                        healthChecks[item.Descriptor.Id] = item.IsRunning
                            ? Adapter.Diagnostics.HealthCheckResult.Healthy(null)
                            : Adapter.Diagnostics.HealthCheckResult.Unhealthy(null, Resources.HealthChecks_AdapterNotRunning);
                        continue;
                    }
                    healthChecks[item.Descriptor.Id] = await feature.CheckHealthAsync(adapterCallContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) {
                    healthChecks[item.Descriptor.Id] = Adapter.Diagnostics.HealthCheckResult.Unhealthy(null, error: e.ToString());
                }
            }

            var resultData = new ReadOnlyDictionary<string, object>(healthChecks.ToDictionary(x => x.Key, x => (object) x.Value));

            var aggregateStatus = Adapter.Diagnostics.HealthCheckResult.GetAggregateHealthStatus(healthChecks.Select(x => x.Value.Status));
            
            if (aggregateStatus.IsGood()) {
                return HealthCheckResult.Healthy(data: resultData);
            }

            if (aggregateStatus.IsUncertain()) {
                return HealthCheckResult.Degraded(data: resultData);
            }

            return HealthCheckResult.Unhealthy(data: resultData);
        }
    }
}
