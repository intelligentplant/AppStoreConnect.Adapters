namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Configuration options for gRPC adapter proxies.
    /// </summary>
    public class GrpcAdapterProxyOptions : AdapterOptions {

        /// <summary>
        /// The ID of the remote adapter to connect to.
        /// </summary>
        public string RemoteId { get; set; }

        /// <summary>
        /// A factory that can be used to set per-call credentials for gRPC calls.
        /// </summary>
        public GetGrpcCallCredentials GetCallCredentials { get; set; }

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        public ExtensionFeatureFactory ExtensionFeatureFactory { get; set; }

    }
}
