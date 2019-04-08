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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore.Hubs {

    [Authorize]
    public class RealTimeDataHub : Hub {

        private readonly IHubContext<RealTimeDataHub> _hubContext;

        private AdapterApiAuthorizationService _authorizationService;

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public RealTimeDataHub(IHubContext<RealTimeDataHub> hubContext, AdapterApiAuthorizationService authorizationService, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _adapterCallContext = adapterCallContext ?? throw new ArgumentNullException(nameof(adapterCallContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


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


        public override Task OnConnectedAsync() {
            Context.Items[typeof(ValueObserver)] = new ValueObserver(Context.ConnectionId, _hubContext);
            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception) {
            if (Context.Items.TryGetValue(typeof(ValueObserver), out var observer)) {
                Context.Items.Remove(typeof(ValueObserver));
                (observer as IDisposable)?.Dispose();
            }

            return base.OnDisconnectedAsync(exception);
        }


        private ValueObserver GetObserver() {
            return Context.Items[typeof(ValueObserver)] as ValueObserver;
        }


        private class ValueObserver : IAdapterObserver<SnapshotTagValue>, IDisposable {

            private bool _isDisposed;

            private readonly string _connectionId;

            private readonly IHubContext<RealTimeDataHub> _hubContext;

            private readonly Dictionary<string, ISnapshotTagValueSubscription> _adapterSubscriptions = new Dictionary<string, ISnapshotTagValueSubscription>(StringComparer.OrdinalIgnoreCase);

            private readonly SemaphoreSlim _adapterSubscriptionsLock = new SemaphoreSlim(1, 1);


            internal ValueObserver(string connectionId, IHubContext<RealTimeDataHub> hubContext) {
                _connectionId = connectionId;
                _hubContext = hubContext;
            }


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
