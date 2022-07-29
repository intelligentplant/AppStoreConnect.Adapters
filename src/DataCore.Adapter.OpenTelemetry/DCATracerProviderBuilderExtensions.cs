using System;

namespace OpenTelemetry.Trace {

    /// <summary>
    /// Extensions for <see cref="TracerProviderBuilder"/>.
    /// </summary>
    public static class DCATracerProviderBuilderExtensions {

        /// <summary>
        /// Adds instrumentation for the <see cref="DataCore.Adapter.Diagnostics.ActivitySourceExtensions.DiagnosticSourceName"/> 
        /// activity source.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="TracerProviderBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="TracerProviderBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TracerProviderBuilder AddDataCoreAdapterInstrumentation(this TracerProviderBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddSource(DataCore.Adapter.Diagnostics.Telemetry.DiagnosticSourceName);

            return builder;
        }

    }
}
