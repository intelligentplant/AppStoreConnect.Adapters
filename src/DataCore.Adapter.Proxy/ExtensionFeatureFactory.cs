using System;

namespace DataCore.Adapter.Proxy {
    /// <summary>
    /// Delegate for creating extension feature implementations on behalf of a proxy.
    /// </summary>
    /// <typeparam name="TProxy">
    ///   The proxy adapter type.
    /// </typeparam>
    /// <param name="featureUriOrName">
    ///   The extension feature URI or name.
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
    public delegate object ExtensionFeatureFactory<TProxy>(
        string featureUriOrName, 
        TProxy proxy
    ) where TProxy : class, IAdapterProxy;
}
