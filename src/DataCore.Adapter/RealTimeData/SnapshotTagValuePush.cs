using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Default <see cref="ISnapshotTagValuePush"/> implementation. 
    /// </summary>
    public class SnapshotTagValuePush : ISnapshotTagValuePush, IDisposable {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The scheduler to use when running background tasks.
        /// </summary>
        protected IBackgroundTaskService Scheduler { get; }

        /// <summary>
        /// The logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Fires when the <see cref="SnapshotTagValuePush"/> is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A cancellation token that will fire when the object is disposed.
        /// </summary>
        protected CancellationToken DisposedToken => _disposedTokenSource.Token;

        /// <summary>
        /// Stream manager options.
        /// </summary>
        private readonly SnapshotTagValuePushOptions _options;

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
            SingleWriter = false
        });

        /// <summary>
        /// Channel that is used to publish changes to subscribed tags.
        /// </summary>
        private readonly Channel<(TagIdentifier Tag, bool Added)> _tagSubscriptionChangesChannel = Channel.CreateUnbounded<(TagIdentifier, bool)>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// All subscriptions.
        /// </summary>
        private readonly List<SnapshotTagValueSubscriptionBase> _subscriptions = new List<SnapshotTagValueSubscriptionBase>();

        /// <summary>
        /// Maps from tag ID to the subscribers for that tag.
        /// </summary>
        private readonly Dictionary<string, HashSet<SnapshotTagValueSubscriptionBase>> _subscriptionsByTagId = new Dictionary<string, HashSet<SnapshotTagValueSubscriptionBase>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// For protecting access to <see cref="_subscriptionsByTagId"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The options.
        /// </param>
        /// <param name="scheduler">
        ///   The scheduler to use when running background tasks.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        public SnapshotTagValuePush(
            SnapshotTagValuePushOptions options, 
            IBackgroundTaskService scheduler,
            ILogger logger
        ) {
            _options = options ?? new SnapshotTagValuePushOptions();
            Scheduler = scheduler ?? BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            Scheduler.QueueBackgroundWorkItem(ProcessTagSubscriptionChangesChannel, DisposedToken);
            Scheduler.QueueBackgroundWorkItem(ProcessValueChangesChannel, DisposedToken);
        }


        /// <summary>
        /// Gets the <see cref="TagIdentifier"/> that corresponds to the specified tag name or ID.
        /// </summary>
        /// <param name="context">
        ///   The call context for the caller.
        /// </param>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the <see cref="TagIdentifier"/> for the tag. If the tag does 
        ///   not exist, or the caller is not authorized to access the tag, the return value will 
        ///   be <see langword="null"/>.
        /// </returns>
        /// <remarks>
        ///   If the <see cref="SnapshotTagValuePushOptions"/> for the manager does not 
        ///   specify a <see cref="SnapshotTagValuePushOptions.TagResolver"/> callback, a 
        ///   <see cref="TagIdentifier"/> using the specified <paramref name="tag"/> as the name 
        ///   and ID will be returned.
        /// </remarks>
        private async ValueTask<TagIdentifier> GetTagIdentifier(IAdapterCallContext context, string tag, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(tag)) {
                return null;
            }
            
            return _options.TagResolver == null
                ? new TagIdentifier(tag, tag)
                : await _options.TagResolver.Invoke(context, tag, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Invoked when a tag is added to a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        private async Task AddTagToSubscription(SnapshotTagValueSubscription subscription, TagIdentifier tag) {
            if (subscription == null || tag == null) {
                return;
            }

            if (_currentValueByTagId.TryGetValue(tag.Id, out var value)) {
                await subscription.ValueReceived(value, DisposedToken).ConfigureAwait(false);
            }

            var isNewSubscription = false;

            _subscriptionsLock.EnterWriteLock();
            try {
                if (!_subscriptionsByTagId.TryGetValue(tag.Id, out var subscribers)) {
                    subscribers = new HashSet<SnapshotTagValueSubscriptionBase>();
                    _subscriptionsByTagId[tag.Id] = subscribers;
                    isNewSubscription = true;
                }

                subscribers.Add(subscription);
                if (isNewSubscription) {
                    _tagSubscriptionChangesChannel.Writer.TryWrite((tag, true));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Invoked when a tag is removed from a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        private Task RemoveTagFromSubscription(SnapshotTagValueSubscription subscription, TagIdentifier tag) {
            if (subscription == null || tag == null) {
                return Task.CompletedTask;
            }

            _subscriptionsLock.EnterWriteLock();
            try {
                if (!_subscriptionsByTagId.TryGetValue(tag.Id, out var subscribers)) {
                    // No subscribers
                    return Task.CompletedTask;
                }

                subscribers.Remove(subscription);
                if (subscribers.Count == 0) {
                    // No subscribers remaining.
                    _subscriptionsByTagId.Remove(tag.Id);
                    _tagSubscriptionChangesChannel.Writer.TryWrite((tag, false));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Invoked when a subscription has been cancelled.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        private void OnSubscriptionCancelled(SnapshotTagValueSubscription subscription) {
            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Remove(subscription);
                foreach (var tag in subscription.GetSubscribedTags()) {
                    if (!_subscriptionsByTagId.TryGetValue(tag.Id, out var subscribersForTag)) {
                        continue;
                    }

                    subscribersForTag.Remove(subscription);
                    if (subscribersForTag.Count == 0) {
                        _subscriptionsByTagId.Remove(tag.Id);
                        _tagSubscriptionChangesChannel.Writer.TryWrite((tag, false));
                    }
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Called when the number of subscribers for a tag changes from zero to one.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        protected virtual void OnTagAddedToSubscription(TagIdentifier tag) {
            _options.OnTagSubscriptionAdded?.Invoke(tag);
        }


        /// <summary>
        /// Called when the number of subscribers for a tag changes from one to zero.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        protected virtual void OnTagRemovedFromSubscription(TagIdentifier tag) {
            _options.OnTagSubscriptionRemoved?.Invoke(tag);
            // Remove current value if we are caching it.
            _currentValueByTagId.TryRemove(tag.Id, out var _);
        }


        /// <summary>
        /// Starts a long-running that that will read and process subscription changes published 
        /// to <see cref="_tagSubscriptionChangesChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the task should exit.
        /// </param>
        /// <returns>
        ///   A long-running task.
        /// </returns>
        private async Task ProcessTagSubscriptionChangesChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                if (!await _tagSubscriptionChangesChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                if (!_tagSubscriptionChangesChannel.Reader.TryRead(out var change) || change.Tag == null) {
                    continue;
                }

                try {
                    if (change.Added) {
                        OnTagAddedToSubscription(change.Tag);
                    }
                    else {
                        OnTagRemovedFromSubscription(change.Tag);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Logger.LogError(
                        e, 
                        Resources.Log_ErrorWhileProcessingSnapshotSubscriptionChange, 
                        change.Tag, 
                        change.Added 
                            ? SubscriptionUpdateAction.Subscribe 
                            : SubscriptionUpdateAction.Unsubscribe
                    );
                }
            }
        }


        /// <summary>
        /// Starts a long-running task that will read values published to <see cref="_masterChannel"/> 
        /// and re-publish them to subscribers for the tag.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the task should exit.
        /// </param>
        /// <returns>
        ///   A long-running task.
        /// </returns>
        private async Task ProcessValueChangesChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                if (!await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                if (!_masterChannel.Reader.TryRead(out var value) || value == null) {
                    continue;
                }

                IEnumerable<SnapshotTagValueSubscriptionBase> subscribers;

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

                if (!subscribers.Any()) {
                    continue;
                }

                foreach (var subscriber in subscribers) {
                    try {
                        var success = await subscriber.ValueReceived(value, cancellationToken).ConfigureAwait(false);
                        if (!success) {
                            Logger.LogTrace(Resources.Log_SnapshotValuePublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                        }
                    }
                    catch (OperationCanceledException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Logger.LogError(e, Resources.Log_SnapshotValuePublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public ISnapshotTagValueSubscription Subscribe(IAdapterCallContext context) {
            var subscription = new SnapshotTagValueSubscription(
                context,
                new SnapshotTagValueSubscriptionOptions() { 
                    TagResolver = GetTagIdentifier,
                    OnTagAdded = AddTagToSubscription,
                    OnTagRemoved = RemoveTagFromSubscription,
                    OnCancelled = OnSubscriptionCancelled
                }
            );

            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Add(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            subscription.Start();
            return subscription;
        }


        /// <summary>
        /// Publishes a value to subscribers.
        /// </summary>
        /// <param name="value">
        ///   The value to publish.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the value was published to subscribers.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> ValueReceived(TagValueQueryResult value, CancellationToken cancellationToken = default) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            // Add the value
            var latestValue = _currentValueByTagId.AddOrUpdate(
                value.TagId, 
                value, 
                (key, prev) => prev.Value.UtcSampleTime > value.Value.UtcSampleTime 
                    ? prev 
                    : value
            );

            if (latestValue != value) {
                // There was already a later value sent for this tag.
                return false;
            }

            _subscriptionsLock.EnterReadLock();
            try {
                if (!_subscriptionsByTagId.ContainsKey(value.TagId)) {
                    // No subscribers for this tag.
                    return false;
                }
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }

            try {
                using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                    await _masterChannel.Writer.WaitToWriteAsync(ctSource.Token).ConfigureAwait(false);
                    return _masterChannel.Writer.TryWrite(value);
                }
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested) {
                    // Cancellation token provided by the caller has fired; rethrow the exception.
                    throw;
                }

                // The stream manager is being disposed.
                return false;
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~SnapshotTagValuePush() {
            Dispose(false);
        }


        /// <summary>
        /// Releases resources held by the <see cref="SnapshotTagValuePush"/>.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the object is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _isDisposed = true;
                _masterChannel.Writer.TryComplete();
                _tagSubscriptionChangesChannel.Writer.TryComplete();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _subscriptionsLock.EnterWriteLock();
                try {
                    _subscriptionsByTagId.Clear();
                    foreach (var subscription in _subscriptions) {
                        subscription.Dispose();
                    }
                    _subscriptions.Clear();
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                    _subscriptionsLock.Dispose();
                }
            }
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValuePush"/>
    /// </summary>
    public class SnapshotTagValuePushOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/>.
        /// </summary>
        public Func<IAdapterCallContext, string, CancellationToken, ValueTask<TagIdentifier>> TagResolver { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a tag changes from zero 
        /// to one.
        /// </summary>
        public Action<TagIdentifier> OnTagSubscriptionAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a tag changes from one 
        /// to zero.
        /// </summary>
        public Action<TagIdentifier> OnTagSubscriptionRemoved { get; set; }


        /// <summary>
        /// Creates a delegate compatible with <see cref="TagResolver"/> using an 
        /// <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="ITagInfo"/> feature to use.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        public static Func<IAdapterCallContext, string, CancellationToken, ValueTask<TagIdentifier>> CreateTagResolver(ITagInfo feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return async (context, tag, cancellationToken) => {
                var ch = feature.GetTags(context, new GetTagsRequest() { 
                    Tags = new [] { tag }
                }, cancellationToken);

                try {
                    return await ch.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch {
#pragma warning restore CA1031 // Do not catch general exception types
                    return null;
                }
            };
        }

    }
}
