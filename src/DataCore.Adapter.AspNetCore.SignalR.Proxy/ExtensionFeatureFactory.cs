using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Delegate for creating extension feature implementations on behalf of a proxy.
    /// </summary>
    /// <param name="featureName">
    ///   The extension feature name.
    /// </param>
    /// <param name="proxy">
    ///   The proxy.
    /// </param>
    /// <returns>
    ///   The implementation of the requested feature. Return <see langword="null"/> if no 
    ///   implementation is available.
    /// </returns>
    /// <remarks>
    ///   If the implementation implements <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>, 
    ///   it will be disposed when the proxy is disposed.
    /// </remarks>
    public delegate object ExtensionFeatureFactory(string featureName, SignalRAdapterProxy proxy); 

}
