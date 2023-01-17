using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Defines telemetry-related properties.
    /// </summary>
    public static class Telemetry {

        /// <summary>
        /// The name of the library's <see cref="ActivitySource"/>, <see cref="Meter"/> and 
        /// <see cref="EventSource"/>.
        /// </summary>
        public const string DiagnosticSourceName = "IntelligentPlant.AppStoreConnect.Adapter";

        /// <summary>
        /// Version number to use for <see cref="ActivitySource"/> and <see cref="Meter"/>.
        /// </summary>
        private static readonly string s_telemetryVersion = typeof(Telemetry).Assembly.GetName().Version.ToString(3);

        /// <summary>
        /// The <see cref="System.Diagnostics.ActivitySource"/> for the library.
        /// </summary>
        public static ActivitySource ActivitySource { get; } = new ActivitySource(DiagnosticSourceName, s_telemetryVersion);

        /// <summary>
        /// The <see cref="System.Diagnostics.Metrics.Meter"/> for the library.
        /// </summary>
        public static Meter Meter { get; } = new Meter(DiagnosticSourceName, s_telemetryVersion);

        /// <summary>
        /// The <see cref="System.Diagnostics.Tracing.EventSource"/> for the library.
        /// </summary>
        public static AdapterEventSource EventSource { get; } = AdapterEventSource.Log;

    }
}
