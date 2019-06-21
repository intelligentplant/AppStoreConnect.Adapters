using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via SignalR.
    /// </summary>
    public class SignalRAdapterProxy : IAdapterProxy {

        /// <summary>
        /// Prefix for standard hub routes.
        /// </summary>
        public const string HubRoutePrefix = "/signalr/data-core/v1.0";

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _adapterId;

        /// <summary>
        /// A factory method that can create a hub connection for the specified hub route.
        /// </summary>
        private readonly Func<string, HubConnection> _connectionFactory;

        /// <summary>
        /// Active connections, indexed by hub route.
        /// </summary>
        private readonly ConcurrentDictionary<string, Task<HubConnection>> _connections = new ConcurrentDictionary<string, Task<HubConnection>>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public AdapterDescriptor Descriptor { get; private set; }

        /// <summary>
        /// Adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <inheritdoc />
        public IAdapterFeaturesCollection Features { get { return _features; } }


        /// <summary>
        /// Creates a new <see cref="SignalRAdapterProxy"/> object.
        /// </summary>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        private SignalRAdapterProxy(SignalRAdapterProxyOptions options) {
            _adapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _connectionFactory = options?.ConnectionFactory ?? throw new ArgumentException("Connection factory is required.", nameof(options));
        }


        /// <summary>
        /// Creates a new <see cref="SignalRAdapterProxy"/> object.
        /// </summary>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter proxy.
        /// </returns>
        public static async Task<SignalRAdapterProxy> Create(SignalRAdapterProxyOptions options, CancellationToken cancellationToken = default) {
            var result = new SignalRAdapterProxy(options);
            await result.Init(cancellationToken).ConfigureAwait(false);

            return result;
        }


        /// <summary>
        /// Creates and starts a hub connection for the specified hub route.
        /// </summary>
        /// <param name="url">
        ///   The hub route.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        private async Task<HubConnection> CreateHubConnection(string url, CancellationToken cancellationToken = default) {
            var connection = _connectionFactory.Invoke(url);
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }


        /// <summary>
        /// Gets or creates an active hub connection for the specified hub route.
        /// </summary>
        /// <param name="url">
        ///   The hub route.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        internal async Task<HubConnection> GetOrCreateHubConnection(string url, CancellationToken cancellationToken = default) {
            var connectionTask = _connections.GetOrAdd(url, k => CreateHubConnection(url, cancellationToken));
            return await connectionTask.ConfigureAwait(false);
        }


        /// <summary>
        /// Initialises the proxy.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform the initialisation.
        /// </returns>
        private async Task Init(CancellationToken cancellationToken = default) {
            var connection = await GetOrCreateHubConnection($"{HubRoutePrefix}/info", cancellationToken).ConfigureAwait(false);
            var descriptor = await connection.InvokeAsync<AdapterDescriptorExtended>("GetAdapter", _adapterId, cancellationToken).ConfigureAwait(false);

            Descriptor = new AdapterDescriptor(descriptor.Id, descriptor.Name, descriptor.Description);
            ProxyAdapterFeature.AddFeaturesToProxy(this, _features, descriptor.Features);
        }


        #region [ IDisposable Support ]

        /// <summary>
        /// Flags if the proxy has been disposed.
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// Disposes of the active hub connections.
        /// </summary>
        /// <returns>
        ///   A task that will dispose of the hub connections.
        /// </returns>
        private async Task DisposeAsync() {
            try {
                foreach (var item in _connections.Values.ToArray()) {
                    var connection = await item.ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally {
                _connections.Clear();
            }
        }


        /// <summary>
        /// Disposes of the proxy.
        /// </summary>
        /// <param name="disposing">
        ///   A flag indicating if the proxy is being disposed or finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (disposing) {
                    DisposeAsync().Wait();
                }

                _isDisposed = true;
            }
        }


        /// <summary>
        /// Disposes of the proxy.
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }

        #endregion
    }
}
