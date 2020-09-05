using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are reported as health check problems")]
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
            var adapters = await _adapterAccessor.GetAllAdapters(null, cancellationToken).ConfigureAwait(false);

            var healthChecks = adapters.Select(x => new {
                Adapter = x,
                HealthCheckResult = Task.Run(async () => { 
                    try {
                        var feature = x.Features.Get<Adapter.Diagnostics.IHealthCheck>(WellKnownFeatures.Diagnostics.HealthCheck);
                        if (feature == null) {
                            return x.IsRunning 
                                ? Adapter.Diagnostics.HealthCheckResult.Healthy(null) 
                                : Adapter.Diagnostics.HealthCheckResult.Unhealthy(null, Resources.HealthChecks_AdapterNotRunning);
                        }
                        return await feature.CheckHealthAsync(null, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        return Adapter.Diagnostics.HealthCheckResult.Unhealthy(null, error: e.ToString());
                    }
                }, cancellationToken)
            }).ToArray();

            await Task.WhenAll(healthChecks.Select(x => x.HealthCheckResult))
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);

            var resultData = new ReadOnlyDictionary<string, object>(healthChecks.ToDictionary(x => x.Adapter.Descriptor.Id, x => (object) x.HealthCheckResult));

            var aggregateStatus = Adapter.Diagnostics.HealthCheckResult.GetAggregateHealthStatus(healthChecks.Select(x => x.HealthCheckResult.Result.Status));
            
            switch (aggregateStatus) {
                case Adapter.Diagnostics.HealthStatus.Healthy:
                    return HealthCheckResult.Healthy(data: resultData);
                case Adapter.Diagnostics.HealthStatus.Degraded:
                    return HealthCheckResult.Degraded(data: resultData);
                default:
                    return HealthCheckResult.Unhealthy(data: resultData);
            }
        }
    }
}
