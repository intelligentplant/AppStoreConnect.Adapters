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
        public const string DefaultHubRoute = "/signalr/data-core/v1.0";

        /// <summary>
        /// Indicates if the client has been diposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The SignalR hub connection for the client.
        /// </summary>
        private readonly HubConnection _hubConnection;

        /// <summary>
        /// When <see langword="true"/>, <see cref="_hubConnection"/> will be disposed when the 
        /// client is disposed.
        /// </summary>
        private readonly bool _disposeConnection;

        /// <summary>
        /// The strongly-typed client for querying the remote host about available adapters.
        /// </summary>
        public AdaptersClient Adapters { get; }

        /// <summary>
        /// The strongly-typed client for querying an adapter's asset model.
        /// </summary>
        public AssetModelBrowserClient AssetModel { get; }

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
        public event Func<Exception, Task> Reconnecting {
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
        public event Func<string, Task> Reconnected {
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hubConnection"/> is <see langword="null"/>.
        /// </exception>
        public AdapterSignalRClient(HubConnection hubConnection, bool disposeConnection) {
            _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
            _disposeConnection = disposeConnection;

            Adapters = new AdaptersClient(this);
            AssetModel = new AssetModelBrowserClient(this);
            Events = new EventsClient(this);
            HostInfo = new HostInfoClient(this);
            TagSearch = new TagSearchClient(this);
            TagValueAnnotations = new TagValueAnnotationsClient(this);
            TagValues = new TagValuesClient(this);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterSignalRClient"/> object.
        /// </summary>
        /// <param name="hubConnection">
        ///   The SignalR hub connection for the client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hubConnection"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The client will not dispose the <paramref name="hubConnection"/> when it is disposed. 
        ///   Use the <see cref="AdapterSignalRClient(HubConnection, bool)"/> constructor overload 
        ///   to override this behaviour.
        /// </remarks>
        public AdapterSignalRClient(HubConnection hubConnection) : this(hubConnection, false) { }


        /// <summary>
        /// Gets the <see cref="HubConnection"/> for the client, optionally starting the 
        /// connection if it is currently in a disconnected state.
        /// </summary>
        /// <param name="startConnection">
        ///   When <see langword="true"/>, the connection will be automatically started if it is 
        ///   in a disconnected state.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the <see cref="HubConnection"/> for the client.
        /// </returns>
        public async Task<HubConnection> GetHubConnection(bool startConnection = true, CancellationToken cancellationToken = default) {
            if (startConnection && _hubConnection.State == HubConnectionState.Disconnected) {
                await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            return _hubConnection;
        }


        /// <summary>
        /// Validates the specified object. This method should be called on any adapter request objects 
        /// prior to passing them to an adapter.
        /// </summary>
        /// <param name="instance">
        ///   The object to validate.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="instance"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="instance"/> is not valid.
        /// </exception>
        protected internal void ValidateObject(object instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            if (_disposeConnection) {
                Task.Run(() => _hubConnection.DisposeAsync()).GetAwaiter().GetResult();
            }
        }


        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            if (_disposeConnection) {
                await _hubConnection.DisposeAsync().ConfigureAwait(false);
            }
        }

    }
}
