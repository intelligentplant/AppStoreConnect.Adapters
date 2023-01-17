using System;

using OpenTelemetry.Metrics;

namespace OpenTelemetry.Resources {

    /// <summary>
    /// Extensions for <see cref="MeterProviderBuilder"/>.
    /// </summary>
    public static class DCAMeterProviderBuilderExtensions {

        /// <summary>
        /// Adds instrumentation for the <see cref="DataCore.Adapter.Diagnostics.Telemetry.DiagnosticSourceName"/> 
        /// meter.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="MeterProviderBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="MeterProviderBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static MeterProviderBuilder AddDataCoreAdapterInstrumentation(this MeterProviderBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddMeter(DataCore.Adapter.Diagnostics.Telemetry.Meter.Name);
            return builder;
        }

    }
}
