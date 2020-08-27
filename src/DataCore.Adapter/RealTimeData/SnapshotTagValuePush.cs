using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Default <see cref="ISnapshotTagValuePush"/> implementation. 
    /// </summary>
    public class SnapshotTagValuePush : ISnapshotTagValuePush, IFeatureHealthCheck, IDisposable {

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
        /// Feature options.
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
        private readonly Channel<(TagValueQueryResult Value, SubscriptionChannel<int, TagIdentifier, TagValueQueryResult>[] Subscribers)> _masterChannel = Channel.CreateUnbounded<(TagValueQueryResult, SubscriptionChannel<int, TagIdentifier, TagValueQueryResult>[])>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        /// <summary>
        /// Channel that is used to publish changes to subscribed tags.
        /// </summary>
        private readonly Channel<(TagIdentifier Tag, bool Added, TaskCompletionSource<bool> Processed)> _topicSubscriptionChangesChannel = Channel.CreateUnbounded<(TagIdentifier, bool, TaskCompletionSource<bool>)>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// Maximum number of concurrent subscriptions.
        /// </summary>
        private readonly int _maxSubscriptionCount;

        /// <summary>
        /// The last subscription ID that was issued.
        /// </summary>
        private int _lastSubscriptionId;

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<int, SubscriptionChannel<int, TagIdentifier, TagValueQueryResult>> _subscriptions = new ConcurrentDictionary<int, SubscriptionChannel<int, TagIdentifier, TagValueQueryResult>>();

        /// <summary>
        /// Maps from tag ID to the subscriber count for that tag.
        /// </summary>
        private readonly Dictionary<string, int> _subscriberCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// For protecting access to <see cref="_subscriberCount"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Emits all values that are published to the internal master channel.
        /// </summary>
        public event Action<TagValueQueryResult> Publish;


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
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
            _maxSubscriptionCount = _options.MaxSubscriptionCount;
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
        protected virtual async ValueTask<TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken) {
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
        private async Task OnSubscriptionAddedInternal(SubscriptionChannel<int, TagIdentifier, TagValueQueryResult> subscription) {
            if (_currentValueByTagId.TryGetValue(subscription.Topic.Id, out var value)) {
                subscription.Publish(value, true);
            }

            var isNewSubscription = false;
            TaskCompletionSource<bool> processed = null;

            _subscriptionsLock.EnterWriteLock();
            try {
                if (!_subscriberCount.TryGetValue(subscription.Topic.Id, out var subscriberCount)) {
                    subscriberCount = 0;
                    isNewSubscription = true;
                }

                _subscriberCount[subscription.Topic.Id] = ++subscriberCount;
                if (isNewSubscription) {
                    processed = new TaskCompletionSource<bool>();
                    _topicSubscriptionChangesChannel.Writer.TryWrite((subscription.Topic, true, processed));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (processed == null) {
                return;
            }

            // Wait for change to be processed.
            await processed.Task.WithCancellation(subscription.CancellationToken).ConfigureAwait(false);

            OnSubscriptionAdded();
        }


        /// <summary>
        /// Invoked when a subscription has been cancelled.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID.
        /// </param>
        private void OnSubscriptionCancelledInternal(int subscriptionId) {
            if (_isDisposed) {
                return;
            }

            if (!_subscriptions.TryRemove(subscriptionId, out var subscription)) {
                return;
            }

            _subscriptionsLock.EnterWriteLock();
            try {
                if (!_subscriberCount.TryGetValue(subscription.Topic.Id, out var subscriberCount)) {
                    return;
                }

                --subscriberCount;

                if (subscriberCount == 0) {
                    _subscriberCount.Remove(subscription.Topic.Id);
                    _topicSubscriptionChangesChannel.Writer.TryWrite((subscription.Topic, false, null));
                }
                else {
                    _subscriberCount[subscription.Topic.Id] = subscriberCount;
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
                subscription.Dispose();
                OnSubscriptionCancelled();
            }
        }


        /// <summary>
        /// Called when the number of subscribers for a tag changes from zero to one.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        protected virtual void OnTagAdded(TagIdentifier tag) {
            _options.OnTagSubscriptionAdded?.Invoke(tag);
        }


        /// <summary>
        /// Called when the number of subscribers for a tag changes from one to zero.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        protected virtual void OnTagRemoved(TagIdentifier tag) {
            _options.OnTagSubscriptionRemoved?.Invoke(tag);
            // Remove current value if we are caching it.
            _currentValueByTagId.TryRemove(tag.Id, out var _);
        }


        /// <summary>
        /// Starts a long-running that that will read and process subscription changes published 
        /// to <see cref="_topicSubscriptionChangesChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the task should exit.
        /// </param>
        /// <returns>
        ///   A long-running task.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are written to associated TaskCompletionSource instances")]
        private async Task ProcessTagSubscriptionChangesChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                if (!await _topicSubscriptionChangesChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                if (!_topicSubscriptionChangesChannel.Reader.TryRead(out var change) || change.Tag == null) {
                    continue;
                }

                try {
                    if (change.Added) {
                        OnTagAdded(change.Tag);
                    }
                    else {
                        OnTagRemoved(change.Tag);
                    }

                    if (change.Processed != null) {
                        change.Processed.TrySetResult(true);
                    }
                }
                catch (Exception e) {
                    if (change.Processed != null) {
                        change.Processed.TrySetException(e);
                    }

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ensures recovery from errors occurring when publishing messages to subscribers")]
        private async Task ProcessValueChangesChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                if (!await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                if (!_masterChannel.Reader.TryRead(out var item)) {
                    continue;
                }

                Publish?.Invoke(item.Value);

                foreach (var subscriber in item.Subscribers) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    try {
                        using (var ct = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, subscriber.CancellationToken)) {
                            var success = subscriber.Publish(item.Value);
                            if (!success) {
                                Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e) {
                        Logger.LogError(e, Resources.Log_PublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public async Task<ChannelReader<TagValueQueryResult>> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            ValidationExtensions.ValidateObject(request);

            if (_maxSubscriptionCount > 0 && _subscriptions.Count >= _maxSubscriptionCount) {
                throw new InvalidOperationException(Resources.Error_TooManySubscriptions);
            }

            var tag = await ResolveTag(context, request.Tag, cancellationToken).ConfigureAwait(false);
            if (tag == null) {
                throw new ArgumentException(string.Format(context?.CultureInfo, Resources.Error_CannotResolveTag, request.Tag), nameof(request));
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = new SubscriptionChannel<int, TagIdentifier, TagValueQueryResult>(
                subscriptionId,
                context,
                Scheduler,
                tag,
                request.PublishInterval,
                new[] { DisposedToken, cancellationToken },
                () => OnSubscriptionCancelledInternal(subscriptionId),
                10
            );
            _subscriptions[subscriptionId] = subscription;

            try {
                await OnSubscriptionAddedInternal(subscription).ConfigureAwait(false);
            }
            catch {
                OnSubscriptionCancelledInternal(subscriptionId);
                throw;
            }

            return subscription.Reader;
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        protected virtual void OnSubscriptionAdded() { }


        /// <summary>
        /// Invoked when a subscription is cancelled.
        /// </summary>
        protected virtual void OnSubscriptionCancelled() { }


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

            var tagIdentifier = new TagIdentifier(value.TagId, value.TagName);
            var subscribers = _subscriptions.Values.Where(x => x.Topic.Equals(tagIdentifier)).ToArray();

            if (subscribers.Length == 0) {
                return false;
            }

            try {
                using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                    await _masterChannel.Writer.WaitToWriteAsync(ctSource.Token).ConfigureAwait(false);
                    return _masterChannel.Writer.TryWrite((value, subscribers));
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
        public Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            int subscribedTagCount;

            _subscriptionsLock.EnterReadLock();
            try {
                subscribedTagCount = _subscriberCount.Count;
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }

            var result = HealthCheckResult.Healthy(nameof(SnapshotTagValuePush), data: new Dictionary<string, string>() {
                { Resources.HealthChecks_Data_SubscriberCount, _subscriptions.Count.ToString(context?.CultureInfo) },
                { Resources.HealthChecks_Data_TagCount, subscribedTagCount.ToString(context?.CultureInfo) }
            });

            return Task.FromResult(result);
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
                _topicSubscriptionChangesChannel.Writer.TryComplete();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                foreach (var subscription in _subscriptions.Values.ToArray()) {
                    subscription.Dispose();
                }
                _subscriptions.Clear();
                _subscriptionsLock.EnterWriteLock();
                try {
                    _subscriberCount.Clear();
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                    _subscriptionsLock.Dispose();
                }
            }
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValuePush"/>.
    /// </summary>
    public class SnapshotTagValuePushOptions {

        /// <summary>
        /// The adapter name to use when creating subscription IDs.
        /// </summary>
        public string AdapterId { get; set; }

        /// <summary>
        /// The maximum number of concurrent subscriptions allowed. When this limit is hit, 
        /// attempts to create additional subscriptions will throw exceptions. A value less than 
        /// one indicates no limit.
        /// </summary>
        public int MaxSubscriptionCount { get; set; }

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Prevent error propagation when a channel closes unexpectedly")]
        public static Func<IAdapterCallContext, string, CancellationToken, ValueTask<TagIdentifier>> CreateTagResolver(ITagInfo feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return async (context, tag, cancellationToken) => {
                var ch = await feature.GetTags(context, new GetTagsRequest() { 
                    Tags = new [] { tag }
                }, cancellationToken).ConfigureAwait(false);

                try {
                    return await ch.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch {
                    return null;
                }
            };
        }

    }
}
