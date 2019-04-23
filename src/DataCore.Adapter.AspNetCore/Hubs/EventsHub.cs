using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;
using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub that is used to push event messages to subscribers. Event message push is only 
    /// supported on adapters that implement the <see cref="IEventMessagePush"/> feature.
    /// </summary>
    public class EventsHub : Hub {

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
        /// Creates a new <see cref="EventsHub"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   Authorization service for controlling access to adapters.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        public EventsHub(AdapterApiAuthorizationService authorizationService, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _adapterCallContext = adapterCallContext ?? throw new ArgumentNullException(nameof(adapterCallContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="active">
        ///   A flag indicating if an active or passive subscription should be created.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> CreateChannel(string adapterId, bool active, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                throw new ArgumentException(string.Format(Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IEventMessagePush>(
                Context.User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                throw new SecurityException();
            }

            var subscription = GetOrAddSubscription(_adapterCallContext, adapter, active, cancellationToken);
            return subscription.Reader;
        }


        /// <summary>
        /// Invoked when a new connection is created.
        /// </summary>
        /// <returns>
        ///   A task that will process the connection.
        /// </returns>
        public override Task OnConnectedAsync() {
            // Store a dictionary of adapter subscriptions in the connection context.
            Context.Items[typeof(IEventMessageSubscription)] = new ConcurrentDictionary<string, IEventMessageSubscription>();
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
            // Remove the adapter subscriptions from the connection context.
            if (Context.Items.TryGetValue(typeof(IEventMessageSubscription), out var o)) {
                Context.Items.Remove(typeof(IEventMessageSubscription));
                if (o is ConcurrentDictionary<string, IEventMessageSubscription> observers) {
                    foreach (var observer in observers.Values.ToArray()) {
                        observer.Dispose();
                    }
                    observers.Clear();
                }
            }

            return base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Gets the current connection's subscription for the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   The <see cref="IEventMessageSubscription"/> for the adapter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        private IEventMessageSubscription GetSubscription(string adapterId) {
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }

            var observerDict = Context.Items[typeof(IEventMessageSubscription)] as ConcurrentDictionary<string, IEventMessageSubscription>;
            return observerDict.TryGetValue(adapterId, out var observer)
                ? observer
                : null;
        }



        /// <summary>
        /// Gets or creates an event subscription to the specified adapter.
        /// </summary>
        /// <param name="callContext">
        ///   The call context.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="active">
        ///   A flag indicating if an active or passive subscription should be created.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   An <see cref="IEventMessageSubscription"/> for the adapter
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   <paramref name="adapter"/> does not support the <see cref="IEventMessagePush"/> feature.
        /// </exception>
        private IEventMessageSubscription GetOrAddSubscription(IAdapterCallContext callContext, IAdapter adapter, bool active, CancellationToken cancellationToken) {
            var feature = adapter.Features.Get<IEventMessagePush>();
            if (feature == null) {
                throw new InvalidOperationException(string.Format(Resources.Error_UnsupportedInterface, nameof(IEventMessagePush)));
            }

            var subscriptionsForConnection = Context.Items[typeof(IEventMessageSubscription)] as ConcurrentDictionary<string, IEventMessageSubscription>;
            return subscriptionsForConnection.GetOrAdd(adapter.Descriptor.Id, key => new EventMessageSubscription(key, feature.Subscribe(callContext, active), subscriptionsForConnection, cancellationToken));
        }


        /// <summary>
        /// Subscription wrapper class.
        /// </summary>
        private class EventMessageSubscription : IEventMessageSubscription {

            /// <summary>
            /// Flags if the subscription has been disposed.
            /// </summary>
            private bool _isDisposed;

            /// <summary>
            /// The adapter ID for the subscription.
            /// </summary>
            private readonly string _adapterId;

            /// <summary>
            /// The inner subscription returned by the adapter.
            /// </summary>
            private readonly IEventMessageSubscription _inner;

            /// <summary>
            /// Automatically disposes the subscription if the caller cancels the streaming request.
            /// </summary>
            private readonly CancellationTokenRegistration _onStreamCancelled;

            /// <summary>
            /// The subscriptions dictionary for the connection.
            /// </summary>
            private readonly ConcurrentDictionary<string, IEventMessageSubscription> _subscriptionsForConnection;


            /// <inheritdoc/>
            ChannelReader<EventMessage> IEventMessageSubscription.Reader {
                get { return _inner.Reader; }
            }


            /// <summary>
            /// Creates a new <see cref="EventMessageSubscription"/> object.
            /// </summary>
            /// <param name="adapterId">
            ///   The adapter ID.
            /// </param>
            /// <param name="inner">
            ///   The inner subscription returned by the adapter.
            /// </param>
            /// <param name="subscriptionsForConnection">
            ///   The subscriptions dictionary for the connection.
            /// </param>
            /// <param name="streamCancelled">
            ///   A cancellation token that will fire if the streaming request is cancelled by the caller.
            /// </param>
            internal EventMessageSubscription(string adapterId, IEventMessageSubscription inner, ConcurrentDictionary<string, IEventMessageSubscription> subscriptionsForConnection, CancellationToken streamCancelled) {
                _adapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _subscriptionsForConnection = subscriptionsForConnection ?? throw new ArgumentNullException(nameof(subscriptionsForConnection));
                _onStreamCancelled = streamCancelled.Register(Dispose);
            }


            /// <inheritdoc/>
            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                _subscriptionsForConnection.TryRemove(_adapterId, out var _);
                _onStreamCancelled.Dispose();
                _inner.Dispose();
                _isDisposed = true;
            }
        }

    }
}
