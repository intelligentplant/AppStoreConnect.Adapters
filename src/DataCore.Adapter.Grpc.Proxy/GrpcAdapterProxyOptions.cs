using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Configuration options for gRPC adapter proxies.
    /// </summary>
    public class GrpcAdapterProxyOptions : AdapterOptions {

        /// <summary>
        /// The ID of the remote adapter to connect to.
        /// </summary>
        [Required]
        public string RemoteId { get; set; } = default!;

        /// <summary>
        /// The interval at which to send a heartbeat message to the remote service.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// A callback that can be used to set per-call credentials for gRPC calls.
        /// </summary>
        public GetGrpcCallCredentials? GetCallCredentials { get; set; }

        /// <summary>
        /// A callback that is used to retrieve <see cref="GrpcCore.Interceptors.Interceptor"/> 
        /// instances to attach to all gRPC clients created by the adapter.
        /// </summary>
        public GetGrpcClientInterceptors? GetClientInterceptors { get; set; }

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public ExtensionFeatureFactory<GrpcAdapterProxy>? ExtensionFeatureFactory { get; set; }

        /// <summary>
        /// When <see langword="true"/>, <see cref="GrpcCore.ChannelBase.ShutdownAsync"/> will be 
        /// called on the channel passed to the adapter's constructor when the adapter is disposed.
        /// </summary>
        public bool CloseChannelOnDispose { get; set; }

    }
}
