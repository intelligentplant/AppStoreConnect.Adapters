using System;
using DataCore.Adapter.AspNetCore.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Extensions for <see cref="IHealthChecksBuilder"/>.
    /// </summary>
    public static class AdapterHealthCheckBuilderExtensions {

        /// <summary>
        /// The name that <see cref="AdapterHealthCheck"/> will be registered under.
        /// </summary>
        public const string AdapterHealthCheckName = "adapter_health_check";


        /// <summary>
        /// Registers adapter health checks with ASP.NET Core.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IHealthChecksBuilder"/> to register the adapter health checks with.
        /// </param>
        /// <returns>
        ///   The <paramref name="builder"/>.
        /// </returns>
        [Obsolete("This method has a typo in the name. Use " + nameof(AddAdapterHealthChecks) + " instead.", false)]
        public static IHealthChecksBuilder AddAdapterHeathChecks(this IHealthChecksBuilder builder) => builder.AddAdapterHealthChecks();


        /// <summary>
        /// Registers adapter health checks with ASP.NET Core.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IHealthChecksBuilder"/> to register the adapter health checks with.
        /// </param>
        /// <returns>
        ///   The <paramref name="builder"/>.
        /// </returns>
        public static IHealthChecksBuilder AddAdapterHealthChecks(this IHealthChecksBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddCheck<AdapterHealthCheck>(AdapterHealthCheckName);
        }

    }
}
