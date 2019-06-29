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
        /// A factory method that gets or creates a connection for the hub endpoint.
        /// </summary>
        public Func<HubConnection> ConnectionFactory { get; set; }

    }
}
