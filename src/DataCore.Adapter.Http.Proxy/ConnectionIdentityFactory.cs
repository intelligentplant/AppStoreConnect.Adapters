using System;
using System.Collections.Generic;

namespace DataCore.Adapter.Http.Proxy {
    /// <summary>
    /// A delegate for extracting the parts used to uniquely identify a SignalR connection from a 
    /// provided <see cref="IAdapterCallContext"/>.
    /// </summary>
    /// <param name="callContext">
    ///   The <see cref="IAdapterCallContext"/>.
    /// </param>
    /// <returns>
    ///   The identifier parts for the <see cref="IAdapterCallContext"/>.
    /// </returns>
    public delegate IEnumerable<string> ConnectionIdentityFactory(IAdapterCallContext callContext); 
}
