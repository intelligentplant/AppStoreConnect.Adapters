using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Options for creating a <see cref="SignalRAdapterProxy"/>.
    /// </summary>
    public class SignalRAdapterProxyOptions {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        public string AdapterId { get; set; }

        /// <summary>
        /// A factory method that creates hub connections on behalf of the proxy.
        /// </summary>
        public ConnectionFactory ConnectionFactory { get; set; }

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        public ExtensionFeatureFactory ExtensionFeatureFactory { get; set; }

    }
}
