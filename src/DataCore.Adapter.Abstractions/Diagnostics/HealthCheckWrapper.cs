using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Wrapper for <see cref="IHealthCheck"/>.
    /// </summary>
    internal class HealthCheckWrapper : AdapterFeatureWrapper<IHealthCheck>, IHealthCheck {

        /// <summary>
        /// Creates a new <see cref="HealthCheckWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal HealthCheckWrapper(AdapterCore adapter, IHealthCheck innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        Task<HealthCheckResult> IHealthCheck.CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            return InvokeAsync(context, InnerFeature.CheckHealthAsync, cancellationToken);
        }


        /// <inheritdoc/>
        IAsyncEnumerable<HealthCheckResult> IHealthCheck.Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, InnerFeature.Subscribe, cancellationToken);
        }

    }
}
