using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Http.Client;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.Http.Proxy {
    /// <summary>
    /// Options for creating a <see cref="HttpAdapterProxy"/>.
    /// </summary>
    public class HttpAdapterProxyOptions : AdapterOptions {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        [Required]
        public string RemoteId { get; set; } = default!;

        /// <summary>
        /// The App Store Connect adapter toolkit version to use when querying the remote adapter.
        /// </summary>
        public CompatibilityVersion CompatibilityVersion { get; set; } = CompatibilityVersion.Latest;

        /// <summary>
        /// The interval to use between re-polling the health status of the remote adapter. 
        /// Ignored if the remote adapter does not support <see cref="Diagnostics.IHealthCheck"/>.
        /// </summary>
        public TimeSpan HealthCheckPushInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The interval to use between re-polling snapshot values for subscribed tags. Ignored if 
        /// the remote adapter does not support <see cref="Adapter.RealTimeData.ISnapshotTagValuePush"/>.
        /// </summary>
        public TimeSpan TagValuePushInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        public ExtensionFeatureFactory<HttpAdapterProxy>? ExtensionFeatureFactory { get; set; }

    }
}
