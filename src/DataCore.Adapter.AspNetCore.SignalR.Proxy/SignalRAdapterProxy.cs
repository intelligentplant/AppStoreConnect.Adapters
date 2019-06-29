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
    public class SignalRAdapterProxy : IAdapterProxy, IDisposable
#if NETCOREAPP3_0
        , 
        IAsyncDisposable
#endif
        {

        /// <summary>
        /// Fires when the proxy is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The relative SignalR hub route.
        /// </summary>
        public const string HubRoute = "/signalr/data-core/v1.0";

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _adapterId;

        /// <summary>
        /// A factory method that can create a hub connection.
        /// </summary>
        private readonly Func<HubConnection> _connectionFactory;

        /// <summary>
        /// The hub connection.
        /// </summary>
        private readonly Lazy<Task<HubConnection>> _connection;
        
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
            _connection = new Lazy<Task<HubConnection>>(() => Task.Run(async () => {
                var conn = _connectionFactory.Invoke();
                await conn.StartAsync(_disposedTokenSource.Token).ConfigureAwait(false);
                return conn;
            }));
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
        /// Gets or creates an active hub connection.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        internal async Task<HubConnection> GetOrCreateHubConnection(CancellationToken cancellationToken = default) {
            var connectionTask = _connection.Value;
            return await connectionTask.WithCancellation(cancellationToken).ConfigureAwait(false);
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
            var connection = await GetOrCreateHubConnection(cancellationToken).ConfigureAwait(false);
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
        public async ValueTask DisposeAsync() {
            if (_isDisposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            try {    
                if (_connection.IsValueCreated) {
                    await _connection.Value.ConfigureAwait(false);
                    await _connection.Value.Result.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                // Do nothing - the connection task was cancelled.
            }

            _isDisposed = true;
        }


        /// <summary>
        /// Disposes of the proxy.
        /// </summary>
        public void Dispose() {
            if (!_isDisposed) {
                DisposeAsync().GetAwaiter().GetResult();
            }
        }

        #endregion
    }
}
