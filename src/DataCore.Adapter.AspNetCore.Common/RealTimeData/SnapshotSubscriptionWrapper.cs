using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.AspNetCore.RealTimeData {
    /// <summary>
    /// Wraps an <see cref="ISnapshotTagValueSubscription"/> to allow individual, disposable 
    /// tag subscriptions to be created.
    /// </summary>
    public sealed class SnapshotSubscriptionWrapper : IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private int _isDisposed;

        /// <summary>
        /// Fires when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The wrapped subscription.
        /// </summary>
        private readonly ISnapshotTagValueSubscription _subscription;

        /// <summary>
        /// Lock for accessing <see cref="_subscribers"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscribersLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Active tag subscriptions, indexed by tag ID.
        /// </summary>
        private readonly Dictionary<string, List<SnapshotTagSubscription>> _subscribers = new Dictionary<string, List<SnapshotTagSubscription>>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Creates a new <see cref="SnapshotSubscriptionWrapper"/> object.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription to wrap.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running subscriptions in 
        ///   background work items. Specify <see langword="null"/> to use 
        ///   <see cref="BackgroundTaskService.Default"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="subscription"/> is <see langword="null"/>.
        /// </exception>
        public SnapshotSubscriptionWrapper(
            ISnapshotTagValueSubscription subscription,
            IBackgroundTaskService backgroundTaskService = null
        ) {
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            (backgroundTaskService ?? BackgroundTaskService.Default).QueueBackgroundWorkItem(ct => _subscription.Reader.ForEachAsync(async item => {
                var subscribers = new List<SnapshotTagSubscription>();
                _subscribersLock.EnterReadLock();
                try {
                    if (_subscribers.TryGetValue(item.TagId, out var idSubscribers)) {
                        subscribers.AddRange(idSubscribers);
                    }
                    if (_subscribers.TryGetValue(item.TagName, out var nameSubscribers)) {
                        subscribers.AddRange(nameSubscribers);
                    }
                }
                finally {
                    _subscribersLock.ExitReadLock();
                }

                foreach (var subscriber in subscribers) {
                    await subscriber.WriteAsync(item, ct).ConfigureAwait(false);
                }
            }, ct), _disposedTokenSource.Token);
        }


        /// <summary>
        /// Gets the total number of subscriptions.
        /// </summary>
        /// <returns>
        ///   The subscription count.
        /// </returns>
        public int GetSubscriptionCount() {
            _subscribersLock.EnterReadLock();
            try {
                return _subscribers.Sum(x => x.Value.Count);
            }
            finally {
                _subscribersLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Adds a subscription to the specified tag.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a new <see cref="SnapshotTagSubscription"/> 
        ///   object. The result value will be <see langword="null"/> if the subscription 
        ///   could not be created.
        /// </returns>
        public async ValueTask<SnapshotTagSubscription> AddSubscription(string tagId) {
            if (string.IsNullOrWhiteSpace(tagId)) {
                return null;
            }

            if (!await _subscription.AddTagToSubscription(tagId).ConfigureAwait(false)) {
                return null;
            }

            var subscription = new SnapshotTagSubscription(tagId, RemoveSubscription);

            _subscribersLock.EnterWriteLock();
            try {
                if (!_subscribers.TryGetValue(tagId, out var subscribers)) {
                    subscribers = new List<SnapshotTagSubscription>();
                    _subscribers[tagId] = subscribers;
                }
                subscribers.Add(subscription);
            }
            finally {
                _subscribersLock.ExitWriteLock();
            }

            return subscription;
        }


        /// <summary>
        /// Removes the specified tag subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The tag subsription.
        /// </param>
        private void RemoveSubscription(SnapshotTagSubscription subscription) {
            _subscribersLock.EnterWriteLock();
            try {
                if (!_subscribers.TryGetValue(subscription.TagId, out var subscribers)) {
                    return;
                }

                subscribers.Remove(subscription);
                if (subscribers.Count == 0) {
                    _subscribers.Remove(subscription.TagId);
                }
            }
            finally {
                _subscribersLock.ExitWriteLock();
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _subscription.Dispose();

            _subscribersLock.EnterWriteLock();
            try {
                foreach (var subscriberList in _subscribers.Values.ToArray()) {
                    foreach (var item in subscriberList.ToArray()) {
                        item.Dispose();
                    }
                }
                _subscribers.Clear();
            }
            finally {
                _subscribersLock.ExitWriteLock();
            }

            _subscribersLock.Dispose();
        }

    }
}
