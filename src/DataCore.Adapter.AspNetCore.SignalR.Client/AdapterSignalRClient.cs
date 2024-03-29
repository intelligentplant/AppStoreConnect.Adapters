﻿#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.SignalR.Client.Clients;

using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client {

    /// <summary>
    /// Client for querying remote adapters via ASP.NET Core SignalR.
    /// </summary>
    public class AdapterSignalRClient : IDisposable, IAsyncDisposable {

        /// <summary>
        /// The relative SignalR hub route.
        /// </summary>
        public const string DefaultHubRoute = "/signalr/app-store-connect/v2.0";

        /// <summary>
        /// Indicates if either <see cref="Dispose()"/> or <see cref="DisposeAsync"/> have been 
        /// called (regardless of whether or not the methods have finished executing).
        /// </summary>
        private bool _disposeCalled;

        /// <summary>
        /// Indicates if <see cref="StopAsync"/> has been called. Automatic reconnection will not 
        /// be attempted if the connection has been explicitly stopped.
        /// </summary>
        private bool _stopCalled;

        /// <summary>
        /// Indicates if the client has been diposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The SignalR hub connection for the client.
        /// </summary>
        private readonly HubConnection _hubConnection;

        /// <summary>
        /// Ensures that only one connection attempt can be made at a time.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncLock _connectionLock = new Nito.AsyncEx.AsyncLock();

        /// <summary>
        /// When <see langword="true"/>, <see cref="_hubConnection"/> will be disposed when the 
        /// client is disposed.
        /// </summary>
        private readonly bool _disposeConnection;

        /// <summary>
        /// <see cref="CancellationTokenSource"/> that will request cancellation when the client is 
        /// disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The ASP.NET Core SignalR compatibility level for the client.
        /// </summary>
        public CompatibilityLevel CompatibilityLevel { get; }

        /// <summary>
        /// The strongly-typed client for querying the remote host about available adapters.
        /// </summary>
        public AdaptersClient Adapters { get; }

        /// <summary>
        /// The strongly-typed client for querying an adapter's asset model.
        /// </summary>
        public AssetModelBrowserClient AssetModel { get; }

        /// <summary>
        /// The strongly-typed client for subscribing to receive configuration change notifications.
        /// </summary>
        public ConfigurationChangesClient ConfigurationChanges { get; }

        /// <summary>
        /// The strongly-typed client for invoking custom functions on an adapter.
        /// </summary>
        public CustomFunctionsClient CustomFunctions { get; }

        /// <summary>
        /// The strongly-typed client for reading event messages from and writing event messages 
        /// to an adapter.
        /// </summary>
        public EventsClient Events { get; }

        /// <summary>
        /// The strongly-typed client for requesting information about the remote host.
        /// </summary>
        public HostInfoClient HostInfo { get; }

        /// <summary>
        /// The strongly-typed client for browsing tags on an adapter.
        /// </summary>
        public TagSearchClient TagSearch { get; }

        /// <summary>
        /// The strongly-typed client for reading tag value annotations from and writing tag value 
        /// annotations to an adapter.
        /// </summary>
        public TagValueAnnotationsClient TagValueAnnotations { get; }

        /// <summary>
        /// The strongly-typed client for reading tag values from and writing tag values to an 
        /// adapter.
        /// </summary>
        public TagValuesClient TagValues { get; }

        /// <summary>
        /// The strongly-typed client for invoking extension features on an adapter.
        /// </summary>
        [Obsolete(Adapter.Extensions.ExtensionFeatureConstants.ObsoleteMessage, Adapter.Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public ExtensionFeaturesClient Extensions { get; }

        /// <summary>
        /// The SignalR client's connection state.
        /// </summary>
        public HubConnectionState ConnectionState => _hubConnection.State;

        /// <summary>
        /// Occurs when the connection is closed.
        /// </summary>
        /// <remarks>
        ///   This event directly maps to the equivalent event on the underlying 
        ///   <see cref="HubConnection"/>.
        /// </remarks>
        public event Func<Exception, Task> Closed {
            add { _hubConnection.Closed += value; }
            remove { _hubConnection.Closed -= value; }
        }

        /// <summary>
        /// Occurs when the client starts reconnecting after losing its underlying connection.
        /// </summary>
        /// <remarks>
        ///   This event directly maps to the equivalent event on the underlying 
        ///   <see cref="HubConnection"/>.
        /// </remarks>
        public event Func<Exception?, Task> Reconnecting {
            add { _hubConnection.Reconnecting += value; }
            remove { _hubConnection.Reconnecting -= value; }
        }

        /// <summary>
        /// Occurs when the client successfully reconnects after losing its underlying connection.
        /// </summary>
        /// <remarks>
        ///   This event directly maps to the equivalent event on the underlying 
        ///   <see cref="HubConnection"/>.
        /// </remarks>
        public event Func<string?, Task> Reconnected {
            add { _hubConnection.Reconnected += value; }
            remove { _hubConnection.Reconnected -= value; }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterSignalRClient"/> object.
        /// </summary>
        /// <param name="hubConnection">
        ///   The SignalR hub connection for the client.
        /// </param>
        /// <param name="disposeConnection">
        ///   When <see langword="true"/>, the <paramref name="hubConnection"/> will be disposed 
        ///   when the client is disposed.
        /// </param>
        /// <param name="compatibilityLevel">
        ///   The compatibility level to use. Specify <see cref="CompatibilityLevel.Latest"/> 
        ///   unless you are connecting to an ASP.NET Core 2.x host application.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hubConnection"/> is <see langword="null"/>.
        /// </exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public AdapterSignalRClient(HubConnection hubConnection, bool disposeConnection, CompatibilityLevel compatibilityLevel = CompatibilityLevel.Latest) {
            _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
            _disposeConnection = disposeConnection;
            CompatibilityLevel = compatibilityLevel;

            Adapters = new AdaptersClient(this);
            AssetModel = new AssetModelBrowserClient(this);
            CustomFunctions = new CustomFunctionsClient(this);
            Events = new EventsClient(this);
            Extensions = new ExtensionFeaturesClient(this);
            HostInfo = new HostInfoClient(this);
            ConfigurationChanges = new ConfigurationChangesClient(this);
            TagSearch = new TagSearchClient(this);
            TagValueAnnotations = new TagValueAnnotationsClient(this);
            TagValues = new TagValuesClient(this);

            Closed += OnClosedAsync;
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters


        /// <summary>
        /// Creates a new <see cref="AdapterSignalRClient"/> object.
        /// </summary>
        /// <param name="hubConnection">
        ///   The SignalR hub connection for the client.
        /// </param>
        /// <param name="compatibilityLevel">
        ///   The compatibility level to use. Specify <see cref="CompatibilityLevel.Latest"/> 
        ///   unless you are connecting to an ASP.NET Core 2.x host application.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hubConnection"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The client will not dispose the <paramref name="hubConnection"/> when it is disposed. 
        ///   Use the <see cref="AdapterSignalRClient(HubConnection, bool, CompatibilityLevel)"/> 
        ///   constructor overload to override this behaviour.
        /// </remarks>
        public AdapterSignalRClient(HubConnection hubConnection, CompatibilityLevel compatibilityLevel = CompatibilityLevel.Latest) : this(hubConnection, false, compatibilityLevel) { }


        /// <summary>
        /// Gets the <see cref="HubConnection"/> for the client and starts the connection if it is 
        /// currently in a disconnected state.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the <see cref="HubConnection"/> for the client.
        /// </returns>
        internal async Task<HubConnection> GetHubConnectionAsync(CancellationToken cancellationToken = default) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (_hubConnection.State == HubConnectionState.Disconnected) {
                await StartConnectionAsync(cancellationToken).ConfigureAwait(false);
            }

            return _hubConnection;
        }


        /// <summary>
        /// Starts the SignalR connection.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will start the connection.
        /// </returns>
        private async Task StartConnectionAsync(CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token))
            using (await _connectionLock.LockAsync(ctSource.Token).ConfigureAwait(false)) {
                if (_hubConnection.State == HubConnectionState.Disconnected) {
                    _stopCalled = false;
                    await _hubConnection.StartAsync(ctSource.Token).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Handles <see cref="Closed"/> events.
        /// </summary>
        /// <param name="error">
        ///   The optional error that caused the connection to be closed.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will re-open the connection if it was not closed due to an 
        ///   explicit call to <see cref="StopAsync"/>, <see cref="Dispose()"/> or <see cref="DisposeAsync"/>.
        /// </returns>
        private async Task OnClosedAsync(Exception? error) {
            if (_disposeCalled || _stopCalled) {
                // Disposing or the connection was closed gracefully.
                return;
            }

            await StartConnectionAsync(default).ConfigureAwait(false);
        }


        /// <summary>
        /// Stops the SignalR connection if it has been started.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will stop the SignalR connection.
        /// </returns>
        public async Task StopAsync(CancellationToken cancellationToken = default) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token))
            using (await _connectionLock.LockAsync(ctSource.Token).ConfigureAwait(false)) {
                if (_hubConnection.State != HubConnectionState.Disconnected) {
                    _stopCalled = true;
                    await _hubConnection.StopAsync(ctSource.Token).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Validates an object. This should be called on all adapter request objects prior to 
        /// invoking a remote endpoint.
        /// </summary>
        /// <param name="o">
        ///   The object.
        /// </param>
        /// <param name="canBeNull">
        ///   When <see langword="true"/>, validation will succeed if <paramref name="o"/> is 
        ///   <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="o"/> is <see langword="null"/> and <paramref name="canBeNull"/> is 
        ///   <see langword="false"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="o"/> fails validation.
        /// </exception>
        public static void ValidateObject(object o, bool canBeNull = false) {
            if (canBeNull && o == null) {
                return;
            }

            if (o == null) {
                throw new ArgumentNullException(nameof(o));
            }

            Validator.ValidateObject(o, new ValidationContext(o));
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~AdapterSignalRClient() {
            Dispose(false);
        }


        /// <summary>
        /// Asynchronously releases managed resources.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that represents the dispose operation.
        /// </returns>
        protected virtual async ValueTask DisposeAsyncCore() {
            if (_isDisposed) {
                return;
            }

            _disposeCalled = true;
            _hubConnection.Closed -= OnClosedAsync;
            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            if (_disposeConnection) {
                using (await _connectionLock.LockAsync().ConfigureAwait(false)) {
                    await _hubConnection.DisposeAsync().ConfigureAwait(false);
                }
            }

            _isDisposed = true;
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the object is being disposed synchronously, or <see langword="false"/> 
        ///   if it is being disposed asynchronously, or finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _disposeCalled = true;
                _hubConnection.Closed -= OnClosedAsync;
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();

                if (_disposeConnection) {
                    _ = Task.Run(async () => {
                        try {
                            using (await _connectionLock.LockAsync().ConfigureAwait(false)) {
                                await _hubConnection.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        catch { }
                    });
                }
            }

            _isDisposed = true;
        }

    }
}

#pragma warning restore CS0618 // Type or member is obsolete
