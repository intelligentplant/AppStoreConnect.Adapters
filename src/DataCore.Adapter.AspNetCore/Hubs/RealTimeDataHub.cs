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
        /// <param name="authorizationService">
        ///   Authorization service for controlling access to adapters.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        public RealTimeDataHub(AdapterApiAuthorizationService authorizationService, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _adapterCallContext = adapterCallContext ?? throw new ArgumentNullException(nameof(adapterCallContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Creates a new snapshot push subscription on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<SnapshotTagValue>> CreateChannel(string adapterId, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
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

            var subscription = GetOrAddSubscription(_adapterCallContext, adapter, cancellationToken);
            return subscription.Reader;
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

            var subscription = GetSubscription(adapter.Descriptor.Id);
            if (subscription == null) {
                return -1;
            }

            return await subscription.AddTagsToSubscription(tagIdsOrNames, Context.ConnectionAborted).ConfigureAwait(false);
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

            var subscription = GetSubscription(adapter.Descriptor.Id);
            if (subscription == null) {
                return -1;
            }

            return await subscription.RemoveTagsFromSubscription(tagIdsOrNames, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Invoked when a new connection is created.
        /// </summary>
        /// <returns>
        ///   A task that will process the connection.
        /// </returns>
        public override Task OnConnectedAsync() {
            // Store a dictionary of adapter subscriptions in the connection context.
            Context.Items[typeof(ISnapshotTagValueSubscription)] = new ConcurrentDictionary<string, ISnapshotTagValueSubscription>();
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
            if (Context.Items.TryGetValue(typeof(ISnapshotTagValueSubscription), out var o)) {
                Context.Items.Remove(typeof(ISnapshotTagValueSubscription));
                if (o is ConcurrentDictionary<string, ISnapshotTagValueSubscription> observers) {
                    foreach (var observer in observers.Values.ToArray()) {
                        observer.Dispose();
                    }
                    observers.Clear();
                }
            }

            return base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Gets the current connection's value observer for the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   The <see cref="ISnapshotTagValueSubscription"/> for the adapter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        private ISnapshotTagValueSubscription GetSubscription(string adapterId) {
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }

            var observerDict =  Context.Items[typeof(ISnapshotTagValueSubscription)] as ConcurrentDictionary<string, ISnapshotTagValueSubscription>;
            return observerDict.TryGetValue(adapterId, out var observer)
                ? observer
                : null;
        }



        /// <summary>
        /// Gets or creates a real-time data subscription to the specified adapter.
        /// </summary>
        /// <param name="callContext">
        ///   The call context.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   A <see cref="ISnapshotTagValueSubscription"/> for the specified adapter.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   <paramref name="adapter"/> does not support the <see cref="ISnapshotTagValuePush"/> feature.
        /// </exception>
        private ISnapshotTagValueSubscription GetOrAddSubscription(IAdapterCallContext callContext, IAdapter adapter, CancellationToken cancellationToken) {
            var feature = adapter.Features.Get<ISnapshotTagValuePush>();
            if (feature == null) {
                throw new InvalidOperationException(string.Format(Resources.Error_UnsupportedInterface, nameof(ISnapshotTagValuePush)));
            }

            var observerDict = Context.Items[typeof(ISnapshotTagValueSubscription)] as ConcurrentDictionary<string, ISnapshotTagValueSubscription>;
            return observerDict.GetOrAdd(adapter.Descriptor.Id, key => feature.Subscribe(callContext));
        }

    }
}
