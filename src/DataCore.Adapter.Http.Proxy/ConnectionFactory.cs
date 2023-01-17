using System;

using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.Http.Proxy {

    /// <summary>
    /// Delegate for creating SignalR hub connections.
    /// </summary>
    /// <param name="url">
    ///   The URL for the connection.
    /// </param>
    /// <param name="context">
    ///   The <see cref="IAdapterCallContext"/> to associated with the connection. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    ///   The configured hub connection.
    /// </returns>
    public delegate HubConnection ConnectionFactory(Uri url, IAdapterCallContext? context);

}
