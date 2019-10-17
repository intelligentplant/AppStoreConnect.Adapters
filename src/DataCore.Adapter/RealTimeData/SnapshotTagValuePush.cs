using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Base class for simplifying implementation of the <see cref="ISnapshotTagValuePush"/> 
    /// feature.
    /// </summary>
    public abstract class SnapshotTagValuePush : ISnapshotTagValuePush, IDisposable {

        /// <summary>
        /// Logging.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A cancellation token that will fire when the <see cref="SnapshotTagValuePush"/> 
        /// is disposed.
        /// </summary>
        protected CancellationToken DisposedToken {
            get { return _disposedTokenSource.Token; }
        }

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
        /// Creates a new <see cref="SnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        protected SnapshotTagValuePush(ILogger logger) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = Task.Factory.StartNew(async () => {
                try {
                    await PublishToSubscribers(_disposedTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInSnapshotSubscriptionManagerPublishLoop);
                }
            }, _disposedTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
        protected internal Channel<TagValueQueryResult> CreateChannel(int capacity, BoundedChannelFullMode fullMode) {
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
            var subscription = new Subscription(this);
            if (subscription == null) {
                throw new InvalidOperationException();
            }
            OnSubscriptionCreated(subscription);

            return Task.FromResult<ISnapshotTagValueSubscription>(subscription);
        }


        /// <summary>
        /// Creates a new subscription.
        /// </summary>
        /// <param name="subscriptionManager">
        ///   The subscription manager that is creating the subscription.
        /// </param>
        /// <returns>
        ///   A new <see cref="Subscription"/> object.
        /// </returns>
        /// <remarks>
        ///   Override this method if you need to customise some features of the snapshot tag 
        ///   value subscription in a subclass (e.g. your adapter supports wildcards in tag names 
        ///   when subscribing, and you need to add custom logic to the 
        ///   <see cref="Subscription.IsSubscribed(TagValueQueryResult)"/> method to handle this).
        /// </remarks>
        protected virtual Subscription CreateSubscription(SnapshotTagValuePush subscriptionManager) {
            return new Subscription(subscriptionManager);
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
        private void OnSubscriptionDisposed(Subscription subscription, IEnumerable<TagIdentifier> subscribedTags) {
            if (_isDisposed) {
                return;
            }

            bool removed;
            _subscriptionsLock.EnterWriteLock();
            try {
                removed = _subscriptions.Remove(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (removed && subscribedTags.Any() && !_isDisposed && !_disposedTokenSource.IsCancellationRequested) {
                return;
            }

            _ = Task.Run(async () => {
                try {
                    await OnTagsRemovedFromSubscription(subscription, subscribedTags, _disposedTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorWhileDisposingOfSnapshotSubscription);
                }
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
        /// Called by a subscription to let the <see cref="SnapshotTagValuePush"/> know 
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
        /// Called by a subscription to let the <see cref="SnapshotTagValuePush"/> know 
        /// that tags were removed from the subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription that was modified.
        /// </param>
        /// <param name="tags">
        ///   The tags that were removed from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will unregister the tag subscriptions.
        /// </returns>
        private async Task OnTagsRemovedFromSubscription(Subscription subscription, IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            var length = tags.Count();
            var tagsToRemove = new List<string>(length);

            foreach (var tag in tags) {
                if (string.IsNullOrWhiteSpace(tag.Id)) {
                    continue;
                }

                _subscriptionsLock.EnterWriteLock();
                try {
                    if (!_subscriptionsByTagId.TryGetValue(tag.Id, out var subscribersForTag)) {
                        continue;
                    }

                    subscribersForTag.Remove(subscription);
                    if (subscribersForTag.Count == 0) {
                        _subscriptionsByTagId.Remove(tag.Id);
                        tagsToRemove.Add(tag.Id);
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
        /// Informs the <see cref="SnapshotTagValuePush"/> that snapshot value changes 
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
        /// Informs the <see cref="SnapshotTagValuePush"/> that a snapshot value change
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
                            subscribers = _subscriptions.Where(x => x.IsSubscribed(value)).ToArray();
                        }
                        finally {
                            _subscriptionsLock.ExitReadLock();
                        }

                        if (subscribers.Length == 0) {
                            continue;
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
        ~SnapshotTagValuePush() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="SnapshotTagValuePush"/> is being 
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
        ///   <see langword="true"/> if the <see cref="SnapshotTagValuePush"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected abstract void Dispose(bool disposing);


        /// <summary>
        /// <see cref="ISnapshotTagValueSubscription"/> implementation.
        /// </summary>
        public class Subscription : SnapshotTagValueSubscription {

            /// <summary>
            /// The subscription manager that created the subscription.
            /// </summary>
            private readonly SnapshotTagValuePush _subscriptionManager;

            /// <inheritdoc/>
            public override int Count {
                get {
                    _subscribedTagsLock.EnterReadLock();
                    try {
                        return _subscribedTags.Count;
                    }
                    finally {
                        _subscribedTagsLock.ExitReadLock();
                    }
                }
            }

            /// <summary>
            /// The tags that have been added to the subscription.
            /// </summary>
            private HashSet<TagIdentifier> _subscribedTags = new HashSet<TagIdentifier>(TagIdentifierComparer.Id);

            /// <summary>
            /// Subscribed tags indexed by ID.
            /// </summary>
            private ILookup<string, TagIdentifier> _subscribedTagsById;

            /// <summary>
            /// Subscribed tags indexed by name.
            /// </summary>
            private ILookup<string, TagIdentifier> _subscribedTagsByName;

            /// <summary>
            /// Lock for accessing field related to tag subscriptions.
            /// </summary>
            private readonly ReaderWriterLockSlim _subscribedTagsLock = new ReaderWriterLockSlim();


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="subscriptionManager">
            ///   The subscription manager.
            /// </param>
            /// <exception cref="ArgumentNullException">
            ///   <paramref name="subscriptionManager"/> is <see langword="null"/>.
            /// </exception>
            protected internal Subscription(SnapshotTagValuePush subscriptionManager) {
                _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            }


            /// <inheritdoc/>
            protected override Channel<TagValueQueryResult> CreateChannel() {
                return _subscriptionManager.CreateChannel(5000, BoundedChannelFullMode.DropOldest);
            }


            /// <inheritdoc/>
            protected override ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                // No additional setup required.
                return default;
            }


            /// <summary>
            /// Gets all tags that the subscription is currently subscribed to.
            /// </summary>
            /// <returns>
            ///   The subscribed tag identifiers.
            /// </returns>
            protected internal IEnumerable<TagIdentifier> GetTags() {
                _subscribedTagsLock.EnterReadLock();
                try {
                    return _subscribedTags.ToArray();
                }
                finally {
                    _subscribedTagsLock.ExitReadLock();
                }
            }


            /// <inheritdoc/>
            public override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
                var result = ChannelExtensions.CreateTagIdentifierChannel();

                result.Writer.RunBackgroundOperation(async (ch, ct) => {
                    foreach (var item in GetTags()) {
                        await ch.WriteAsync(item, ct).ConfigureAwait(false);
                    }
                });

                return result;
            }


            /// <inheritdoc/>
            public override async Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                tagNamesOrIds = tagNamesOrIds
                    ?.Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();

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

                _subscribedTagsLock.EnterWriteLock();
                try {
                    newTags = tagIdentifiers.Where(x => _subscribedTags.Add(x)).ToArray();
                    if (newTags.Length > 0) {
                        RebuildLookups();
                    }
                }
                finally {
                    _subscribedTagsLock.ExitWriteLock();
                }

                if (newTags.Length > 0) {
                    OnTagsAddedToSubscription(newTags);
                    await _subscriptionManager.OnTagsAddedToSubscription(this, newTags, cancellationToken).ConfigureAwait(false);
                }

                return Count;
            }


            /// <summary>
            /// Called when tags are added to the subscription.
            /// </summary>
            /// <param name="tags">
            ///   The tags that were added.
            /// </param>
            /// <remarks>
            ///   Override this method to perform custom logic in a subclass when tags are added 
            ///   to a subscription.
            /// </remarks>
            protected virtual void OnTagsAddedToSubscription(IEnumerable<TagIdentifier> tags) {
                // Do nothing.
            }


            /// <inheritdoc/>
            public override async Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                tagNamesOrIds = tagNamesOrIds
                    ?.Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();

                if (!tagNamesOrIds.Any()) {
                    return Count;
                }

                var removed = new List<TagIdentifier>(tagNamesOrIds.Count());

                _subscribedTagsLock.EnterWriteLock();
                try {
                    foreach (var tag in tagNamesOrIds) {
                        // Try and remove by tag ID.
                        var existing = _subscribedTags.FirstOrDefault(x => string.Equals(x.Id, tag, StringComparison.Ordinal));
                        if (existing != null) {
                            _subscribedTags.Remove(existing);
                            removed.Add(existing);
                            continue;
                        }

                        // Now try and remove by tag name.
                        existing = _subscribedTags.FirstOrDefault(x => string.Equals(x.Name, tag, StringComparison.OrdinalIgnoreCase));
                        if (existing != null) {
                            _subscribedTags.Remove(existing);
                            removed.Add(existing);
                        }
                    }

                    if (removed.Count > 0) {
                        RebuildLookups();
                    }
                }
                finally {
                    _subscribedTagsLock.ExitWriteLock();
                }

                if (removed.Count > 0) {
                    OnTagsRemovedFromSubscription(removed);
                    await _subscriptionManager.OnTagsRemovedFromSubscription(this, removed, cancellationToken).ConfigureAwait(false);
                }

                return Count;
            }


            /// <summary>
            /// Called when tags are removed from the subscription.
            /// </summary>
            /// <param name="tags">
            ///   The tags that were removed.
            /// </param>
            /// <remarks>
            ///   Override this method to perform custom logic in a subclass when tags are added 
            ///   to a subscription.
            /// </remarks>
            protected virtual void OnTagsRemovedFromSubscription(IEnumerable<TagIdentifier> tags) {
                // Do nothing.
            }


            /// <summary>
            /// Sends a value change to the observer.
            /// </summary>
            /// <param name="value">
            ///   The updated value.
            /// </param>
            internal async Task OnValueChanged(TagValueQueryResult value) {
                if (SubscriptionCancelled.IsCancellationRequested) {
                    return;
                }

                await Writer.WriteAsync(value, SubscriptionCancelled).ConfigureAwait(false);
            }


            /// <summary>
            /// Rebuilds the subscribed tags lookups. Assumes that a write lock has already been 
            /// obtained from <see cref="_subscribedTagsLock"/>.
            /// </summary>
            private void RebuildLookups() {
                _subscribedTagsById = _subscribedTags.ToLookup(x => x.Id);
                _subscribedTagsByName = _subscribedTags.ToLookup(x => x.Name);
            }


            /// <summary>
            /// Tests if the subscription object holds a subscription for the specified tag value.
            /// </summary>
            /// <param name="value">
            ///   The tag value.
            /// </param>
            /// <returns>
            ///   <see langword="true"/> if a subscription is held for the tag value.
            /// </returns>
            protected internal virtual bool IsSubscribed(TagValueQueryResult value) {
                _subscribedTagsLock.EnterReadLock();
                try {
                    return (_subscribedTagsById?.Contains(value.TagId) ?? false) ||
                           (_subscribedTagsByName?.Contains(value.TagName) ?? false);
                }
                finally {
                    _subscribedTagsLock.ExitReadLock();
                }
            }


            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _subscriptionManager.OnSubscriptionDisposed(this, _subscribedTags.ToArray());
                    _subscribedTagsLock.Dispose();
                    _subscribedTags.Clear();
                }
            }


            /// <inheritdoc />
            protected override ValueTask DisposeAsync(bool disposing) {
                Dispose(disposing);
                return new ValueTask();
            }


        }

    }

}
