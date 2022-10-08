using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Extensions;
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
        ///The SignalR options for the proxy.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If <see cref="SignalROptions"/> is <see langword="null"/>, SignalR functionality 
        ///   will be disabled.
        /// </para>
        /// 
        /// <para>
        ///   When <see cref="CompatibilityVersion"/> is <see cref="CompatibilityVersion.Version_3_0"/> 
        ///   or higher, SignalR capabilities will only be enabled if the remote host spcifies 
        ///   that the adapter SignalR API is enabled. If a lower <see cref="CompatibilityVersion"/> 
        ///   is specified, SignalR capabilities will always be enabled when a <see cref="SignalROptions"/> 
        ///   is specified.
        /// </para>
        /// 
        /// </remarks>
        public SignalROptions? SignalROptions { get; set; }

        /// <summary>
        /// The interval to use between re-polling the health status of the remote adapter. 
        /// Ignored if the remote adapter does not support <see cref="Adapter.Diagnostics.IHealthCheck"/>.
        /// </summary>
        /// <remarks>
        ///   Specifying a value less than or equal to <see cref="TimeSpan.Zero"/> will result in 
        ///   periodic health check updates being disabled.
        /// </remarks>
        public TimeSpan HealthCheckPushInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The interval to use between re-polling snapshot values for subscribed tags. Ignored if 
        /// the remote adapter does not support <see cref="Adapter.RealTimeData.IReadSnapshotTagValues"/>.
        /// </summary>
        /// <remarks>
        ///   Specifying a value less than or equal to <see cref="TimeSpan.Zero"/> will result in 
        ///   the <see cref="Adapter.RealTimeData.ISnapshotTagValuePush"/> feature being disabled, 
        ///   even if the remote adapter supports <see cref="Adapter.RealTimeData.IReadSnapshotTagValues"/>.
        /// </remarks>
        public TimeSpan TagValuePushInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public ExtensionFeatureFactory<HttpAdapterProxy>? ExtensionFeatureFactory { get; set; }

    }
}
