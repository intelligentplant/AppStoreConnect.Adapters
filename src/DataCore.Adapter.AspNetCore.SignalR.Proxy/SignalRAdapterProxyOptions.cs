using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Options for creating a <see cref="SignalRAdapterProxy"/>.
    /// </summary>
    public class SignalRAdapterProxyOptions : AdapterOptions {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        [Required]
        public string RemoteId { get; set; } = default!;

        /// <summary>
        /// The SignalR compatibility level to use.
        /// </summary>
        public CompatibilityLevel CompatibilityLevel { get; set; } = CompatibilityLevel.Latest;

        /// <summary>
        /// A factory method that creates hub connections on behalf of the proxy.
        /// </summary>
        [Required]
        public ConnectionFactory ConnectionFactory { get; set; } = default!;

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        public ExtensionFeatureFactory<SignalRAdapterProxy>? ExtensionFeatureFactory { get; set; }

    }
}
