using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Delegate for creating SignalR hub connections on behalf of a proxy.
    /// </summary>
    /// <param name="key">
    ///   The key for the connection. The value will be <see langword="null"/> unless a connection is 
    ///   being requested for an extension feature.
    /// </param>
    /// <returns>
    ///   The configured hub connection.
    /// </returns>
    public delegate HubConnection ConnectionFactory(string key = null);

}
