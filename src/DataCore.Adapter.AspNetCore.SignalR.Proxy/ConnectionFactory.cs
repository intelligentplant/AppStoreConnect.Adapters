using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Delegate for creating SignalR hub connections.
    /// </summary>
    /// <param name="key">
    ///   The identifier for the connection. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    ///   The configured hub connection.
    /// </returns>
    public delegate HubConnection ConnectionFactory(string? key = null);

}
