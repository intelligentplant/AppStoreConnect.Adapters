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
        /// <param name="tags">
        ///   The tags to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateChannel(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken) {
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

            var subscription = await GetOrCreateSubscription(_adapterCallContext, adapter, cancellationToken).ConfigureAwait(false);

            tags = tags
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? new string[0];

            if (tags.Any()) {
                await subscription.AddTagsToSubscription(_adapterCallContext, tags, cancellationToken).ConfigureAwait(false);
            }

            return subscription.Reader;
        }


        public async Task<int> AddTagsToSubscription(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken) {
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

            tags = tags
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? new string[0];

            if (!tags.Any()) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagIsRequired, nameof(tags));
            }

            var subscription = GetSubscription(adapter);
            if (subscription == null) {
                throw new ArgumentException(Resources.Error_AdapterSubscriptionDoesNotExist, nameof(adapterId));
            }

            return await subscription.AddTagsToSubscription(_adapterCallContext, tags, cancellationToken).ConfigureAwait(false);
        }


        public async Task<int> RemoveTagsFromSubscription(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken) {
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

            tags = tags
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? new string[0];

            if (!tags.Any()) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagIsRequired, nameof(tags));
            }

            var subscription = GetSubscription(adapter);
            if (subscription == null) {
                throw new ArgumentException(Resources.Error_AdapterSubscriptionDoesNotExist, nameof(adapterId));
            }

            return await subscription.RemoveTagsFromSubscription(_adapterCallContext, tags, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Invoked when a new connection is created.
        /// </summary>
        /// <returns>
        ///   A task that will process the connection.
        /// </returns>
        public override Task OnConnectedAsync() {
            // Store a dictionary of adapter subscriptions in the connection context.
            Context.Items[typeof(ISnapshotTagValueSubscription)] = new List<SubscriptionWrapper>();
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
                if (o is List<SubscriptionWrapper> observers) {
                    lock (observers) {
                        foreach (var observer in observers.ToArray()) {
                            observer.Dispose();
                        }
                        observers.Clear();
                    }
                }
            }

            return base.OnDisconnectedAsync(exception);
        }



        /// <summary>
        /// Gets or creates a real-time data subscription on the specified adapter.
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
        ///   An <see cref="ISnapshotTagValueSubscription"/> for the specified adapter.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   <paramref name="adapter"/> does not support the <see cref="ISnapshotTagValuePush"/> feature.
        /// </exception>
        private async Task<ISnapshotTagValueSubscription> GetOrCreateSubscription(IAdapterCallContext callContext, IAdapter adapter, CancellationToken cancellationToken) {
            var subscription = GetSubscription(adapter);
            if (subscription != null) {
                return subscription;
            }

            var feature = adapter.Features.Get<ISnapshotTagValuePush>();
            if (feature == null) {
                throw new InvalidOperationException(string.Format(Resources.Error_UnsupportedInterface, nameof(ISnapshotTagValuePush)));
            }

            var subscriptionsForConnection = Context.Items[typeof(ISnapshotTagValueSubscription)] as List<SubscriptionWrapper>;
            subscription = await feature.Subscribe(callContext, cancellationToken).ConfigureAwait(false);

            SubscriptionWrapper result;
            lock (subscriptionsForConnection) {
                result = new SubscriptionWrapper(adapter.Descriptor.Id, subscription, subscriptionsForConnection, cancellationToken);
                subscriptionsForConnection.Add(result);
            }

            return result;
        }


        /// <summary>
        /// Gets an existing real-time data subscription on the specified adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The <see cref="ISnapshotTagValueSubscription"/> for the specified adapter, or 
        ///   <see langword="null"/> if a subscription does not exist.
        /// </returns>
        private ISnapshotTagValueSubscription GetSubscription(IAdapter adapter) {
            var subscriptionsForConnection = Context.Items[typeof(ISnapshotTagValueSubscription)] as List<SubscriptionWrapper>;
            return subscriptionsForConnection?.FirstOrDefault(x => string.Equals(x.AdapterId, adapter.Descriptor.Id));
        }


        private class SubscriptionWrapper : ISnapshotTagValueSubscription {

            public string AdapterId { get; }

            private readonly ISnapshotTagValueSubscription _inner;

            private Action _onDisposed;

            private readonly CancellationTokenRegistration _onStreamCancelled;


            /// <inheritdoc/>
            public ChannelReader<TagValueQueryResult> Reader { get { return _inner.Reader; } }

            /// <inheritdoc/>
            public int Count { get { return _inner.Count; } }


            public SubscriptionWrapper(string adapterId, ISnapshotTagValueSubscription inner, ICollection<SubscriptionWrapper> subscriptionsForConnection, CancellationToken streamCancelled) {
                AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _onDisposed = () => {
                    lock (subscriptionsForConnection) {
                        subscriptionsForConnection.Remove(this);
                    }
                };
                streamCancelled.Register(Dispose);
            }


            /// <inheritdoc/>
            public Task<IEnumerable<TagIdentifier>> GetTags(CancellationToken cancellationToken) {
                return _inner.GetTags(cancellationToken);
            }


            /// <inheritdoc/>
            public Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                return _inner.AddTagsToSubscription(context, tagNamesOrIds, cancellationToken);
            }


            /// <inheritdoc/>
            public Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                return _inner.RemoveTagsFromSubscription(context, tagNamesOrIds, cancellationToken);
            }


            public void Dispose() {
                _onDisposed.Invoke();
                _onDisposed = null;
                _onStreamCancelled.Dispose();
                _inner.Dispose();
            }

        }

    }
}
