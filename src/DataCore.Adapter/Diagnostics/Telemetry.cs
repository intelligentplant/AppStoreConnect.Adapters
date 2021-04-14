using System.Diagnostics;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Defines telemetry-related properties.
    /// </summary>
    public static class Telemetry {

        /// <summary>
        /// The name to use for the <see cref="ActivitySource"/> and <see cref="EventSource"/>.
        /// </summary>
        public static string DiagnosticSourceName => ActivitySourceExtensions.DiagnosticSourceName;

        /// <summary>
        /// The <see cref="System.Diagnostics.ActivitySource"/> for the library.
        /// </summary>
        public static ActivitySource ActivitySource { get; } = new ActivitySource(DiagnosticSourceName, typeof(Telemetry).Assembly.GetName().Version.ToString(3));

        /// <summary>
        /// The <see cref="System.Diagnostics.Tracing.EventSource"/> for the library.
        /// </summary>
        public static AdapterEventSource EventSource { get; } = new AdapterEventSource();

    }
}
