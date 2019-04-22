using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Base class for simplifying implementation of the <see cref="Features.ISnapshotTagValuePush"/> 
    /// feature.
    /// </summary>
    public abstract class SnapshotTagValueSubscriptionManager : ISnapshotTagValuePush, IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Holds the current values for subscribed tags.
        /// </summary>
        private readonly ConcurrentDictionary<string, SnapshotTagValue> _currentValueByTagId = new ConcurrentDictionary<string, SnapshotTagValue>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Channel that is used to publish new snapshot values. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<SnapshotTagValue> _masterChannel = Channel.CreateUnbounded<SnapshotTagValue>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// All subscriptions.
        /// </summary>
        private readonly List<Subscription> _subscriptions = new List<Subscription>();

        /// <summary>
        /// Maps from tag ID to the subscribers for that tag.
        /// </summary>
        private readonly Dictionary<string, HashSet<Subscription>> _subscriptionsByTagId = new Dictionary<string, HashSet<Subscription>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// For protecting access to <see cref="_subscriptionsByTagId"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscriptionManager"/> object.
        /// </summary>
        protected SnapshotTagValueSubscriptionManager() {
            _ = Task.Factory.StartNew(async () => {
                try {
                    await PublishToSubscribers(_disposedTokenSource.Token).ConfigureAwait(false);
                }
                catch { }
            }, TaskCreationOptions.LongRunning);
        }


        /// <inheritdoc/>
        public ISnapshotTagValueSubscription Subscribe(IAdapterCallContext adapterCallContext) {
            if (adapterCallContext == null) {
                throw new ArgumentNullException(nameof(adapterCallContext));
            }

            var subscription = new Subscription(this, adapterCallContext);
            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Add(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            return subscription;
        }


        /// <summary>
        /// Informs the <see cref="SnapshotTagValueSubscriptionManager"/> that a subscription has been 
        /// disposed.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription that was disposed.
        /// </param>
        /// <param name="subscribedTags">
        ///   The tags that the subscription was subscribed to.
        /// </param>
        private void OnSubscriptionDisposed(Subscription subscription, IEnumerable<string> subscribedTags) {
            if (_isDisposed) {
                return;
            }

            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Remove(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (!subscribedTags.Any()) {
                return;
            }

            _ = Task.Run(async () => {
                try {
                    await OnUnsubscribeInternal(subscription, subscribedTags, _disposedTokenSource.Token).ConfigureAwait(false);
                }
                catch { }
            });
        }


        /// <summary>
        /// Gets the IDs of all tags that are currently being subscribed to. Implementers can use this 
        /// method when they need to e.g. reinitialise a real-time push connection to a historian after 
        /// a connection outage.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The IDs of all subscribed tags.
        /// </returns>
        protected Task<IEnumerable<string>> GetSubscribedTags(CancellationToken cancellationToken) {
            var result = _subscriptionsByTagId.Keys.ToArray();
            return Task.FromResult<IEnumerable<string>>(result);
        }


        /// <summary>
        /// Called by a subscription to let the <see cref="SnapshotTagValueSubscriptionManager"/> that 
        /// tags were added to the subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription that was modified.
        /// </param>
        /// <param name="tagIds">
        ///   The tags that were added to the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will register the tag subscriptions and return the initial values for the tags.
        /// </returns>
        private async Task OnSubscribeInternal(Subscription subscription, IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            var length = tagIds.Count();
            var newTags = new List<string>(length);

            foreach (var tagId in tagIds) {
                if (string.IsNullOrWhiteSpace(tagId)) {
                    continue;
                }

                if (_currentValueByTagId.TryGetValue(tagId, out var value)) {
                    await subscription.OnValueChanged(value).ConfigureAwait(false);
                }

                _subscriptionsLock.EnterWriteLock();
                try {
                    if (!_subscriptionsByTagId.TryGetValue(tagId, out var subscribersForTag)) {
                        subscribersForTag = new HashSet<Subscription>();
                        _subscriptionsByTagId[tagId] = subscribersForTag;
                    }

                    if (subscribersForTag.Count == 0) {
                        newTags.Add(tagId);
                    }

                    subscribersForTag.Add(subscription);
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }
            }

            if (newTags.Count > 0) {
                var newVals = await OnSubscribe(newTags, cancellationToken).ConfigureAwait(false);
                OnValuesChanged(newVals);
            }
        }


        /// <summary>
        /// Called by a subscription to let the <see cref="SnapshotTagValueSubscriptionManager"/> know 
        /// that tags were removed from the subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription that was modified.
        /// </param>
        /// <param name="tagIds">
        ///   The tags that were removed from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will unregister the tag subscriptions.
        /// </returns>
        private async Task OnUnsubscribeInternal(Subscription subscription, IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            var length = tagIds.Count();
            var tagsToRemove = new List<string>(length);

            foreach (var tagId in tagIds) {
                if (string.IsNullOrWhiteSpace(tagId)) {
                    continue;
                }

                _subscriptionsLock.EnterWriteLock();
                try {
                    if (!_subscriptionsByTagId.TryGetValue(tagId, out var subscribersForTag)) {
                        continue;
                    }

                    subscribersForTag.Remove(subscription);
                    if (subscribersForTag.Count == 0) {
                        _subscriptionsByTagId.Remove(tagId);
                        tagsToRemove.Add(tagId);
                    }
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }
            }

            if (tagsToRemove.Count > 0) {
                await OnUnsubscribe(tagsToRemove, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Informs the <see cref="SnapshotTagValueSubscriptionManager"/> that snapshot value changes 
        /// have occurred.
        /// </summary>
        /// <param name="values">
        ///   The value changes.
        /// </param>
        protected void OnValuesChanged(IEnumerable<SnapshotTagValue> values) {
            if (values == null || !values.Any()) {
                return;
            }

            var valuesToEmit = new List<SnapshotTagValue>(values.Count());
            foreach (var value in values) {
                if (value == null) {
                    continue;
                }
                if (_disposedTokenSource.IsCancellationRequested) {
                    break;
                }

                if (_currentValueByTagId.TryGetValue(value.TagId, out var previousValue)) {
                    if (previousValue.Value.UtcSampleTime >= value.Value.UtcSampleTime) {
                        continue;
                    }
                }

                _currentValueByTagId[value.TagId] = value;
                valuesToEmit.Add(value);
            }

            foreach (var value in values) {
                _masterChannel.Writer.TryWrite(value);
            }
        }


        /// <summary>
        /// Long-running task that sends tag values to subscribers whenever they are added to the 
        /// <see cref="_masterChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires
        /// </returns>
        private async Task PublishToSubscribers(CancellationToken cancellationToken) {
            try {
                while (await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (_masterChannel.Reader.TryRead(out var value)) {
                        if (value == null) {
                            continue;
                        }

                        Subscription[] subscribers;

                        _subscriptionsLock.EnterReadLock();
                        try {
                            if (!_subscriptionsByTagId.TryGetValue(value.TagId, out var subs) || subs.Count == 0) {
                                continue;
                            }

                            subscribers = subs.ToArray();
                        }
                        finally {
                            _subscriptionsLock.ExitReadLock();
                        }

                        await Task.WhenAll(subscribers.Select(x => x.OnValueChanged(value))).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancellation token fired
            }
            catch (ChannelClosedException) {
                // Channel was closed
            }
        }


        /// <summary>
        /// Gets the <see cref="TagIdentifier"/> descriptors for the specified tag names or IDs.
        /// </summary>
        /// <param name="context">
        ///   The adapter call context for the caller.
        /// </param>
        /// <param name="tagNamesOrIds">
        ///   The tag names or IDs to look up.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching <see cref="TagIdentifier"/> object.
        /// </returns>
        protected abstract Task<IEnumerable<TagIdentifier>> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);


        /// <summary>
        /// Called whenever the total subscriber count for a tag changes from zero to greater than zero.
        /// </summary>
        /// <param name="tagIds">
        ///   The tag IDs that have been subscribed to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform any additional subscription setup actions required by the 
        ///   adapter and return the initial values of the subscribed tags.
        /// </returns>
        protected abstract Task<IEnumerable<SnapshotTagValue>> OnSubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken);


        /// <summary>
        /// Called whenever the total subscriber count for a tag changes from greater than zero to zero.
        /// </summary>
        /// <param name="tagIds">
        ///   The tag IDs that have been unsubscribed from.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform any additional subscription teardown actions required by the 
        ///   adapter.
        /// </returns>
        protected abstract Task OnUnsubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken);


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            DisposeInternal(true);

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~SnapshotTagValueSubscriptionManager() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="SnapshotTagValueSubscriptionManager"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        private void DisposeInternal(bool disposing) {
            try {
                Dispose(disposing);
            }
            finally {
                _masterChannel.Writer.TryComplete();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();

                _subscriptionsLock.EnterWriteLock();
                try {
                    foreach (var item in _subscriptions.ToArray()) {
                        item.Dispose();
                    }
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }
                _subscriptionsLock.Dispose();
            }
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="SnapshotTagValueSubscriptionManager"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected abstract void Dispose(bool disposing);


        /// <summary>
        /// Equality comparer for <see cref="TagIdentifier"/> that tests for equality based solely 
        /// on the <see cref="TagIdentifier.Id"/> property.
        /// </summary>
        private class TagIdentifierComparer : IEqualityComparer<TagIdentifier> {

            /// <summary>
            /// Default comparer instance.
            /// </summary>
            public static TagIdentifierComparer Default { get; } = new TagIdentifierComparer();


            /// <summary>
            /// Tests two <see cref="TagIdentifier"/> instances for equality.
            /// </summary>
            /// <param name="x">
            ///   The first instance to compare.
            /// </param>
            /// <param name="y">
            ///   The second instance to compare.
            /// </param>
            /// <returns>
            ///   <see langword="true"/> if the instances are equal, or <see langword="false"/> 
            ///   otherwise.
            /// </returns>
            public bool Equals(TagIdentifier x, TagIdentifier y) {
                if (x == null && y == null) {
                    return true;
                }
                if (x == null || y == null) {
                    return false;
                }

                return string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
            }


            /// <summary>
            /// Gets the hash code for the specific <see cref="TagIdentifier"/> instance.
            /// </summary>
            /// <param name="obj">
            ///   The instance.
            /// </param>
            /// <returns>
            ///   The hash code for the instance.
            /// </returns>
            public int GetHashCode(TagIdentifier obj) {
                if (obj == null) {
                    throw new ArgumentNullException(nameof(obj));
                }

                return obj.GetHashCode();
            }
        }


        /// <summary>
        /// <see cref="ISnapshotTagValueSubscription"/> implementation.
        /// </summary>
        private class Subscription : ISnapshotTagValueSubscription {

            /// <summary>
            /// Flags if the object has been disposed.
            /// </summary>
            private bool _isDisposed;

            /// <summary>
            /// Fires when then object is being disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

            /// <summary>
            /// The subscription manager that created the subscription.
            /// </summary>
            private readonly SnapshotTagValueSubscriptionManager _subscriptionManager;

            /// <summary>
            /// The call context to use when adding tags to the subscription.
            /// </summary>
            private readonly IAdapterCallContext _adapterCallContext;

            /// <summary>
            /// The channel for the subscription.
            /// </summary>
            private readonly Channel<SnapshotTagValue> _channel;

            /// <inheritdoc/>
            public ChannelReader<SnapshotTagValue> Reader {
                get { return _channel.Reader; }
            }

            /// <summary>
            /// The tags that have been added to the subscription.
            /// </summary>
            private readonly HashSet<TagIdentifier> _subscribedTags = new HashSet<TagIdentifier>(TagIdentifierComparer.Default);

            /// <summary>
            /// Lock for modifying the subscribed tags.
            /// </summary>
            private readonly SemaphoreSlim _subscriptionLock = new SemaphoreSlim(1, 1);


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="subscriptionManager">
            ///   The subscription manager.
            /// </param>
            /// <param name="adapterCallContext">
            ///   The call context to use when adding tags to the subscription.
            /// </param>
            internal Subscription(SnapshotTagValueSubscriptionManager subscriptionManager, IAdapterCallContext adapterCallContext) {
                _subscriptionManager = subscriptionManager;
                _adapterCallContext = adapterCallContext;
                _channel = Channel.CreateUnbounded<SnapshotTagValue>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                });
            }


            /// <inheritdoc/>
            public async Task<IEnumerable<TagIdentifier>> GetSubscribedTags(CancellationToken cancellationToken) {
                if (_isDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    return _subscribedTags.ToArray();
                }
                finally {
                    _subscriptionLock.Release();
                }
            }


            /// <inheritdoc/>
            public async Task<int> AddTagsToSubscription(IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                if (_isDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (tagNamesOrIds == null) {
                    throw new ArgumentNullException(nameof(tagNamesOrIds));
                }

                var result = 0;

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    var tagIdentifiers = await _subscriptionManager.GetTags(_adapterCallContext, tagNamesOrIds, cancellationToken).ConfigureAwait(false);
                    var newTags = new List<string>(tagIdentifiers.Count());
                    var dirty = false;

                    if (tagIdentifiers != null) {
                        foreach (var item in tagIdentifiers) {
                            if (item == null) {
                                continue;
                            }
                            var added = _subscribedTags.Add(item);
                            if (added) {
                                dirty = true;
                                newTags.Add(item.Id);
                            }
                        }
                    }

                    if (dirty) {
                        await _subscriptionManager.OnSubscribeInternal(this, newTags, cancellationToken).ConfigureAwait(false);
                    }
                    result = _subscribedTags.Count;
                }
                finally {
                    _subscriptionLock.Release();
                }

                return result;
            }


            /// <inheritdoc/>
            public async Task<int> RemoveTagsFromSubscription(IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                if (_isDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (tagNamesOrIds == null) {
                    throw new ArgumentNullException(nameof(tagNamesOrIds));
                }

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    var tagsToRemove = new List<string>(tagNamesOrIds.Count());

                    foreach (var item in tagNamesOrIds) {
                        if (item == null) {
                            continue;
                        }

                        var tagIdentifier = _subscribedTags.FirstOrDefault(x => string.Equals(item, x.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(item, x.Name, StringComparison.OrdinalIgnoreCase));
                        if (tagIdentifier == null) {
                            continue;
                        }

                        tagsToRemove.Add(tagIdentifier.Id);
                        _subscribedTags.Remove(tagIdentifier);
                    }

                    if (tagsToRemove.Count > 0) {
                        await _subscriptionManager.OnUnsubscribeInternal(this, tagsToRemove, cancellationToken).ConfigureAwait(false);
                    }
                    return _subscribedTags.Count;
                }
                finally {
                    _subscriptionLock.Release();
                }
            }


            /// <summary>
            /// Sends a value change to the observer.
            /// </summary>
            /// <param name="value">
            ///   The updated value.
            /// </param>
            internal async Task OnValueChanged(SnapshotTagValue value) {
                if (_isDisposed || _disposedTokenSource.IsCancellationRequested || _channel.Reader.Completion.IsCompleted) {
                    return;
                }

                await _channel.Writer.WriteAsync(value, _disposedTokenSource.Token).ConfigureAwait(false);
            }


            /// <summary>
            /// Disposes of the subscription.
            /// </summary>
            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                _channel.Writer.TryComplete();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _subscriptionLock.Wait();
                try {
                    _subscriptionManager.OnSubscriptionDisposed(this, _subscribedTags.Select(x => x.Id).ToArray());
                }
                finally {
                    _subscriptionLock.Release();
                }
                _subscriptionLock.Dispose();
                _subscribedTags.Clear();
                _isDisposed = true;
            }
        }

    }
}
