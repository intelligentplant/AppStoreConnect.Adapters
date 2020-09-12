﻿using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Proxy;

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
        /// A factory that can be used to set per-call credentials for gRPC calls.
        /// </summary>
        public GetGrpcCallCredentials GetCallCredentials { get; set; } = default!;

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        public ExtensionFeatureFactory<GrpcAdapterProxy> ExtensionFeatureFactory { get; set; } = default!;

    }
}
