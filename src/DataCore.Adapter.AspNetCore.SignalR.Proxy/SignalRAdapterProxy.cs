using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via SignalR.
    /// </summary>
    public class SignalRAdapterProxy : IAdapterProxy, IDisposable
#if NETSTANDARD2_1
        , 
        IAsyncDisposable
#endif
        {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

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
        /// A factory delegate that can create hub connections.
        /// </summary>
        private readonly ConnectionFactory _connectionFactory;

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;

        /// <summary>
        /// The main hub connection.
        /// </summary>
        private readonly Lazy<Task<HubConnection>> _connection;

        /// <summary>
        /// Additional hub connections created for extension features.
        /// </summary>
        private readonly ConcurrentDictionary<string, Lazy<Task<HubConnection>>> _extensionConnections = new ConcurrentDictionary<string, Lazy<Task<HubConnection>>>();
        
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
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        private SignalRAdapterProxy(SignalRAdapterProxyOptions options, ILogger<SignalRAdapterProxy> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _connectionFactory = options?.ConnectionFactory ?? throw new ArgumentException("Connection factory is required.", nameof(options));
            _connection = new Lazy<Task<HubConnection>>(() => Task.Run(async () => {
                var conn = _connectionFactory.Invoke(null);
                await conn.StartAsync(_disposedTokenSource.Token).ConfigureAwait(false);
                return conn;
            }), LazyThreadSafetyMode.ExecutionAndPublication);
            _extensionFeatureFactory = options?.ExtensionFeatureFactory;
        }


        /// <summary>
        /// Creates a new <see cref="SignalRAdapterProxy"/> object.
        /// </summary>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter proxy.
        /// </returns>
        public static async Task<SignalRAdapterProxy> Create(SignalRAdapterProxyOptions options, ILogger<SignalRAdapterProxy> logger, CancellationToken cancellationToken = default) {
            var result = new SignalRAdapterProxy(options, logger);
            await result.Init(cancellationToken).ConfigureAwait(false);

            return result;
        }


        /// <summary>
        /// Gets or creates an active hub connection for use with standard adapter features.
        /// </summary>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        /// <remarks>
        ///   The connection lifetime is managed by the proxy.
        /// </remarks>
        public Task<HubConnection> GetOrCreateHubConnection() {
            var connectionTask = _connection.Value;
            return connectionTask;
        }


        /// <summary>
        /// Gets or creates an active hub connection for use with an adapter extension feature.
        /// </summary>
        /// <param name="key">
        ///   The key for the extension hub. This cannot be <see langword="null"/> and will be 
        ///   vendor-specific.
        /// </param>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        /// <remarks>
        ///   The connection lifetime is managed by the proxy.
        /// </remarks>
        public Task<HubConnection> GetOrCreateExtensionHubConnection(string key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return _extensionConnections.GetOrAdd(key, k => new Lazy<Task<HubConnection>>(() => Task.Run(async () => {
                var conn = _connectionFactory.Invoke(k);
                await conn.StartAsync(_disposedTokenSource.Token).ConfigureAwait(false);
                return conn;
            }), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
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
            var connection = await GetOrCreateHubConnection().WithCancellation(cancellationToken).ConfigureAwait(false);
            var descriptor = await connection.InvokeAsync<AdapterDescriptorExtended>("GetAdapter", _adapterId, cancellationToken).ConfigureAwait(false);

            Descriptor = new AdapterDescriptor(descriptor.Id, descriptor.Name, descriptor.Description);
            ProxyAdapterFeature.AddFeaturesToProxy(this, _features, descriptor.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in descriptor.Extensions) {
                    if (string.IsNullOrWhiteSpace(extensionFeature)) {
                        continue;
                    }

                    try {
                        var impl = _extensionFeatureFactory.Invoke(extensionFeature, this);
                        if (impl == null) {
                            _logger.LogWarning(Resources.Log_NoExtensionImplementationAvailable, extensionFeature);
                            continue;
                        }

                        _features.AddFromProvider(impl, addStandardFeatures: false);
                    }
                    catch (Exception e) {
                        _logger.LogError(e, Resources.Log_ExtensionFeatureRegistrationError, extensionFeature);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default) {
            await Init(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default) {
            if (_connection.IsValueCreated) {
                await _connection.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
                await _connection.Value.Result.StopAsync(cancellationToken).ConfigureAwait(false);
            }
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
                await StopAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // Do nothing - the connection task was cancelled.
            }

            await _features.DisposeAsync().ConfigureAwait(false);

            _isDisposed = true;
        }


        /// <summary>
        /// Disposes of the proxy.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            try {
                if (_connection.IsValueCreated) {
                    _connection.Value.Result.DisposeAsync().GetAwaiter().GetResult();
                }
            }
            catch (AggregateException) {
                // Do nothing - the connection task was cancelled.
            }

            _features.Dispose();
            _isDisposed = true;
        }

        #endregion
    }
}
