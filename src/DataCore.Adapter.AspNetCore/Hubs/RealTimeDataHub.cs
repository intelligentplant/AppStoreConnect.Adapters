using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub that is used to push real-time snapshot value changes to subscribers. Snapshot push 
    /// is only supported on adapters that implement the <see cref="ISnapshotTagValuePush"/> feature.
    /// </summary>
    public class RealTimeDataHub : Hub {

        /// <summary>
        /// Hub context for sending messages back to connected clients.
        /// </summary>
        private readonly IHubContext<RealTimeDataHub> _hubContext;

        /// <summary>
        /// Authorization service for controlling access to adapters.
        /// </summary>
        private AdapterApiAuthorizationService _authorizationService;

        /// <summary>
        /// The adapter call context describing the calling user.
        /// </summary>
        private readonly IAdapterCallContext _adapterCallContext;

        /// <summary>
        /// For accessing runtime adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="RealTimeDataHub"/> object.
        /// </summary>
        /// <param name="hubContext">
        ///   Hub context for sending messages back to connected clients.
        /// </param>
        /// <param name="authorizationService">
        ///   Authorization service for controlling access to adapters.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        public RealTimeDataHub(IHubContext<RealTimeDataHub> hubContext, AdapterApiAuthorizationService authorizationService, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _adapterCallContext = adapterCallContext ?? throw new ArgumentNullException(nameof(adapterCallContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Subscribes the caller to the specified tags.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to subscribe to. The adapter must support the 
        ///   <see cref="ISnapshotTagValuePush"/> feature.
        /// </param>
        /// <param name="tagIdsOrNames">
        ///   The IDs or names of the tags to subscribe to.
        /// </param>
        /// <returns>
        ///   The total number of tag subscriptions held by the caller after the subscription change.
        /// </returns>
        public async Task<int> AddTagSubscriptions(string adapterId, string[] tagIdsOrNames) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            if (adapter == null) {
                throw new ArgumentException(string.Format(Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            var authResponse = await _authorizationService.AuthorizeAsync<ISnapshotTagValuePush>(
                Context.User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                throw new SecurityException();
            }

            var observer = GetObserver();
            return await observer.AddTagsToSubscription(adapter, _adapterCallContext, tagIdsOrNames, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Unsubscribes the caller from the specified tags.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to unsubscribe from. The adapter must support the 
        ///   <see cref="ISnapshotTagValuePush"/> feature.
        /// </param>
        /// <param name="tagIdsOrNames">
        ///   The IDs or names of the tags to unsubscribe from.
        /// </param>
        /// <returns>
        ///   The total number of tag subscriptions held by the caller after the subscription change.
        /// </returns>
        public async Task<int> RemoveTagSubscriptions(string adapterId, string[] tagIdsOrNames) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            if (adapter == null) {
                throw new ArgumentException(string.Format(Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            var authResponse = await _authorizationService.AuthorizeAsync<ISnapshotTagValuePush>(
                Context.User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                throw new SecurityException();
            }

            var observer = GetObserver();
            return await observer.RemoveTagsFromSubscription(adapter, _adapterCallContext, tagIdsOrNames, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Invoked when a new connection is created.
        /// </summary>
        /// <returns>
        ///   A task that will process the connection.
        /// </returns>
        public override Task OnConnectedAsync() {
            // Store an observer for the connection in the connection context.
            Context.Items[typeof(ValueObserver)] = new ValueObserver(Context.ConnectionId, _hubContext);
            return base.OnConnectedAsync();
        }


        /// <summary>
        /// Invoked when a connection is closed.
        /// </summary>
        /// <param name="exception">
        ///   Non-null if disconnection was due to an error.
        /// </param>
        /// <returns>
        ///   A task that will process the disconnection.
        /// </returns>
        public override Task OnDisconnectedAsync(Exception exception) {
            // Remove the observer for the connection from the connection context.
            if (Context.Items.TryGetValue(typeof(ValueObserver), out var observer)) {
                Context.Items.Remove(typeof(ValueObserver));
                (observer as IDisposable)?.Dispose();
            }

            return base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Gets the value observer for the current connection.
        /// </summary>
        /// <returns>
        ///   The <see cref="ValueObserver"/> for the connection.
        /// </returns>
        private ValueObserver GetObserver() {
            return Context.Items[typeof(ValueObserver)] as ValueObserver;
        }


        /// <summary>
        /// Class for observing snapshot value changes on adapters.
        /// </summary>
        private class ValueObserver : IAdapterObserver<SnapshotTagValue>, IDisposable {

            /// <summary>
            /// Flags if the observer has been disposed.
            /// </summary>
            private bool _isDisposed;

            /// <summary>
            /// The SignalR connection ID for the observer.
            /// </summary>
            private readonly string _connectionId;

            /// <summary>
            /// The hub context to use when pushing values back to the SignalR client.
            /// </summary>
            private readonly IHubContext<RealTimeDataHub> _hubContext;

            /// <summary>
            /// Adapter subscriptions for the observer, indexed by adapter ID.
            /// </summary>
            private readonly Dictionary<string, ISnapshotTagValueSubscription> _adapterSubscriptions = new Dictionary<string, ISnapshotTagValueSubscription>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Lock for accessing <see cref="_adapterSubscriptions"/>.
            /// </summary>
            private readonly SemaphoreSlim _adapterSubscriptionsLock = new SemaphoreSlim(1, 1);


            /// <summary>
            /// Creates a new <see cref="ValueObserver"/> object.
            /// </summary>
            /// <param name="connectionId">
            ///   The connection ID for the observer.
            /// </param>
            /// <param name="hubContext">
            ///   The hub context to use when pushing values back to the SignalR client.
            /// </param>
            internal ValueObserver(string connectionId, IHubContext<RealTimeDataHub> hubContext) {
                _connectionId = connectionId;
                _hubContext = hubContext;
            }


            /// <inheritdoc/>
            public async Task<int> AddTagsToSubscription(IAdapter adapter, IAdapterCallContext context, string[] tagNamesOrIds, CancellationToken cancellationToken) {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();
                if (feature == null) {
                    throw new InvalidOperationException(string.Format(Resources.Error_UnsupportedInterface, nameof(ISnapshotTagValuePush)));
                }

                ISnapshotTagValueSubscription subscription;
                await _adapterSubscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    if (!_adapterSubscriptions.TryGetValue(adapter.Descriptor.Id, out subscription)) {
                        subscription = await feature.Subscribe(context, this, cancellationToken).ConfigureAwait(false);
                        _adapterSubscriptions[adapter.Descriptor.Id] = subscription;
                    }
                }
                finally {
                    _adapterSubscriptionsLock.Release();
                }

                return await subscription.AddTagsToSubscription(tagNamesOrIds, cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            public async Task<int> RemoveTagsFromSubscription(IAdapter adapter, IAdapterCallContext context, string[] tagNamesOrIds, CancellationToken cancellationToken) {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();
                if (feature == null) {
                    throw new InvalidOperationException(string.Format(Resources.Error_UnsupportedInterface, nameof(ISnapshotTagValuePush)));
                }

                ISnapshotTagValueSubscription subscription;
                await _adapterSubscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    if (!_adapterSubscriptions.TryGetValue(adapter.Descriptor.Id, out subscription)) {
                        return 0;
                    }
                }
                finally {
                    _adapterSubscriptionsLock.Release();
                }

                return await subscription.RemoveTagsFromSubscription(tagNamesOrIds, cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            public async Task OnNext(AdapterDescriptor adapter, SnapshotTagValue value) {
                if (_isDisposed) {
                    return;
                }

                await _hubContext
                    .Clients
                    .Client(_connectionId)
                    .SendAsync("Next", adapter.Id, value.TagId, value.TagName, value.Value)
                    .ConfigureAwait(false);                
            }


            /// <inheritdoc/>
            public async Task OnError(AdapterDescriptor adapter, Exception error) {
                if (_isDisposed) {
                    return;
                }

                await _hubContext
                    .Clients
                    .Client(_connectionId)
                    .SendAsync("Error", adapter.Id, error.Message)
                    .ConfigureAwait(false);
            }


            /// <inheritdoc/>
            public async Task OnCompleted(AdapterDescriptor adapter) {
                if (_isDisposed) {
                    return;
                }

                await _hubContext
                    .Clients
                    .Client(_connectionId)
                    .SendAsync("Completed", adapter.Id)
                    .ConfigureAwait(false);
            }


            /// <summary>
            /// Disposes of the observer.
            /// </summary>
            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                foreach (var item in _adapterSubscriptions.Values) {
                    item.Dispose();
                }
                _adapterSubscriptions.Clear();
                _adapterSubscriptionsLock.Dispose();

                _isDisposed = true;
            }
        }

    }
}
