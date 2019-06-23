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
        private readonly ConcurrentDictionary<string, TagValueQueryResult> _currentValueByTagId = new ConcurrentDictionary<string, TagValueQueryResult>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Channel that is used to publish new snapshot values. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<TagValueQueryResult> _masterChannel = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
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


        /// <summary>
        /// Creates a channel that can be used to write snapshot values to.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. Specify less than 1 for an unbounded channel.
        /// </param>
        /// <param name="fullMode">
        ///   The action to take if the channel reaches capacity. Ignored if <paramref name="capacity"/> is less than 1.
        /// </param>
        /// <returns>
        ///   A new channel.
        /// </returns>
        protected Channel<TagValueQueryResult> CreateChannel(int capacity, BoundedChannelFullMode fullMode) {
            return capacity > 0
                ? Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(capacity) {
                    FullMode = fullMode,
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                })
                : Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                });
        }


        /// <inheritdoc/>
        public Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext adapterCallContext, CancellationToken cancellationToken) {
            if (adapterCallContext == null) {
                throw new ArgumentNullException(nameof(adapterCallContext));
            }

            var subscription = new Subscription(this);
            OnSubscriptionCreated(subscription);

            return Task.FromResult<ISnapshotTagValueSubscription>(subscription);
        }


        /// <summary>
        /// Called when a subscription is created.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        private void OnSubscriptionCreated(Subscription subscription) {
            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Add(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Called when a subscription is disposed.
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
                    await OnTagsRemovedFromSubscription(subscription, subscribedTags, _disposedTokenSource.Token).ConfigureAwait(false);
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
        /// Called by a subscription to let the <see cref="SnapshotTagValueSubscriptionManager"/> know 
        /// that tags were added to the subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription that was modified.
        /// </param>
        /// <param name="tags">
        ///   The tags that were added to the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will register the tag subscriptions.
        /// </returns>
        private async Task OnTagsAddedToSubscription(Subscription subscription, IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            var length = tags.Count();
            var newTags = new List<string>(length);

            foreach (var tag in tags) {
                if (_currentValueByTagId.TryGetValue(tag.Id, out var value)) {
                    await subscription.OnValueChanged(value).ConfigureAwait(false);
                }

                _subscriptionsLock.EnterWriteLock();
                try {
                    if (!_subscriptionsByTagId.TryGetValue(tag.Id, out var subscribersForTag)) {
                        subscribersForTag = new HashSet<Subscription>();
                        _subscriptionsByTagId[tag.Id] = subscribersForTag;
                    }

                    if (subscribersForTag.Count == 0) {
                        newTags.Add(tag.Id);
                    }

                    subscribersForTag.Add(subscription);
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }
            }

            if (newTags.Count > 0) {
                await OnSubscribe(newTags, cancellationToken).ConfigureAwait(false);
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
        private async Task OnTagsRemovedFromSubscription(Subscription subscription, IEnumerable<string> tagIds, CancellationToken cancellationToken) {
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
        protected void OnValuesChanged(IEnumerable<TagValueQueryResult> values) {
            if (values == null || !values.Any()) {
                return;
            }

            foreach (var value in values) {
                if (_disposedTokenSource.IsCancellationRequested) {
                    break;
                }

                OnValueChanged(value);
            }
        }


        /// <summary>
        /// Informs the <see cref="SnapshotTagValueSubscriptionManager"/> that a snapshot value change
        /// has occurred.
        /// </summary>
        /// <param name="value">
        ///   The new value.
        /// </param>
        protected void OnValueChanged(TagValueQueryResult value) {
            if (value == null) {
                return;
            }

            if (_currentValueByTagId.TryGetValue(value.TagId, out var previousValue)) {
                if (previousValue.Value.UtcSampleTime >= value.Value.UtcSampleTime) {
                    return;
                }
            }

            _currentValueByTagId[value.TagId] = value;
            _masterChannel.Writer.TryWrite(value);
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
        ///   A reader that returns the matching <see cref="TagIdentifier"/> object.
        /// </returns>
        protected abstract ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);


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
        ///   adapter. Implementers should use <see cref="OnValueChanged(TagValueQueryResult)"/> or 
        ///   <see cref="OnValuesChanged(IEnumerable{TagValueQueryResult})"/> to register the 
        ///   initial value of each tag with the subscription manager.
        /// </returns>
        protected abstract Task OnSubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken);


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
            /// The channel for the subscription.
            /// </summary>
            private readonly Channel<TagValueQueryResult> _channel;

            /// <inheritdoc/>
            public ChannelReader<TagValueQueryResult> Reader {
                get { return _channel.Reader; }
            }

            /// <inheritdoc/>
            public int Count {
                get {
                    lock (_subscribedTags) {
                        return _subscribedTags.Count;
                    }
                }
            }

            /// <summary>
            /// The tags that have been added to the subscription.
            /// </summary>
            private HashSet<TagIdentifier> _subscribedTags { get; } = new HashSet<TagIdentifier>(TagIdentifierComparer.Default);


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="subscriptionManager">
            ///   The subscription manager.
            /// </param>
            internal Subscription(SnapshotTagValueSubscriptionManager subscriptionManager) {
                _subscriptionManager = subscriptionManager;
                _channel = _subscriptionManager.CreateChannel(5000, BoundedChannelFullMode.DropOldest);
            }


            /// <inheritdoc/>
            public ChannelReader<TagIdentifier> GetTags(CancellationToken cancellationToken) {
                var result = ChannelExtensions.CreateTagIdentifierChannel();

                result.Writer.RunBackgroundOperation(async (ch, ct) => {
                    foreach (var item in _subscribedTags.ToArray()) {
                        await ch.WriteAsync(item, ct).ConfigureAwait(false);
                    }
                });

                return result;
            }


            /// <inheritdoc/>
            public async Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                tagNamesOrIds = tagNamesOrIds
                    ?.Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? new string[0];

                if (!tagNamesOrIds.Any()) {
                    return Count;
                }

                var tagsReader = _subscriptionManager.GetTags(context, tagNamesOrIds, cancellationToken);
                var tagIdentifiers = new List<TagIdentifier>();

                while (await tagsReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!tagsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    tagIdentifiers.Add(tag);
                }

                if (tagIdentifiers.Count == 0) {
                    return Count;
                }

                TagIdentifier[] newTags;
                lock (_subscribedTags) {
                    newTags = tagIdentifiers.Where(x => _subscribedTags.Add(x)).ToArray();
                }

                if (newTags.Length > 0) {
                    await _subscriptionManager.OnTagsAddedToSubscription(this, newTags, cancellationToken).ConfigureAwait(false);
                }

                return Count;
            }


            /// <inheritdoc/>
            public async Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                tagNamesOrIds = tagNamesOrIds
                    ?.Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? new string[0];

                if (!tagNamesOrIds.Any()) {
                    return Count;
                }

                var removed = new List<string>(tagNamesOrIds.Count());

                lock (_subscribedTags) {
                    foreach (var tag in tagNamesOrIds) {
                        // Try and remove by tag ID.
                        if (_subscribedTags.Remove(new TagIdentifier(tag, tag))) {
                            removed.Add(tag);
                            continue;
                        }

                        var existingTagByName = _subscribedTags.FirstOrDefault(x => string.Equals(x.Name, tag, StringComparison.OrdinalIgnoreCase));
                        if (existingTagByName != null) {
                            _subscribedTags.Remove(existingTagByName);
                            removed.Add(existingTagByName.Id);
                        }
                    }
                }

                if (removed.Count > 0) {
                    await _subscriptionManager.OnTagsRemovedFromSubscription(this, removed, cancellationToken).ConfigureAwait(false);
                }

                return Count;
            }


            /// <summary>
            /// Sends a value change to the observer.
            /// </summary>
            /// <param name="value">
            ///   The updated value.
            /// </param>
            internal async Task OnValueChanged(TagValueQueryResult value) {
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
                _subscriptionManager.OnSubscriptionDisposed(this, _subscribedTags.Select(x => x.Id).ToArray());
                _subscribedTags.Clear();
                _isDisposed = true;
            }
        }

    }
}
