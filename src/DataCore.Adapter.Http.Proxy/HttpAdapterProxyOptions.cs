using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.Http.Proxy {
    /// <summary>
    /// Options for creating a <see cref="HttpAdapterProxy"/>.
    /// </summary>
    public class HttpAdapterProxyOptions : AdapterOptions {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        [Required]
        public string RemoteId { get; set; }

        /// <summary>
        /// A factory method that the proxy calls to request a concrete implementation of an 
        /// extension feature.
        /// </summary>
        public ExtensionFeatureFactory ExtensionFeatureFactory { get; set; }

    }
}
