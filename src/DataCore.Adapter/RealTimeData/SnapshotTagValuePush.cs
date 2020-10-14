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
    public class SnapshotTagValuePush : ISnapshotTagValuePush, IFeatureHealthCheck, IBackgroundTaskServiceProvider, IDisposable {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </summary>
        public IBackgroundTaskService BackgroundTaskService { get; }

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
        private readonly Channel<(TagValueQueryResult Value, TagValueSubscriptionChannel<int>[] Subscribers)> _masterChannel = Channel.CreateUnbounded<(TagValueQueryResult, TagValueSubscriptionChannel<int>[])>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        /// <summary>
        /// Channel that is used to publish changes to subscribed tags.
        /// </summary>
        private readonly Channel<(List<TagIdentifier> Tags, bool Added, TaskCompletionSource<bool> Processed)> _topicSubscriptionChangesChannel = Channel.CreateUnbounded<(List<TagIdentifier>, bool, TaskCompletionSource<bool>)>(new UnboundedChannelOptions() {
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
        private readonly ConcurrentDictionary<int, TagValueSubscriptionChannel<int>> _subscriptions = new ConcurrentDictionary<int, TagValueSubscriptionChannel<int>>();

        /// <summary>
        /// Maps from tag ID to the subscriber count for that tag.
        /// </summary>
        private readonly Dictionary<TagIdentifier, int> _subscriberCount = new Dictionary<TagIdentifier, int>(TagIdentifierComparer.Id);

        /// <summary>
        /// For protecting access to <see cref="_subscriberCount"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Publishes all values passed to the <see cref="SnapshotTagValuePush"/> via the 
        /// <see cref="ValueReceived"/> method.
        /// </summary>
        public event Action<TagValueQueryResult>? Publish;


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        public SnapshotTagValuePush(
            SnapshotTagValuePushOptions? options,
            IBackgroundTaskService? backgroundTaskService,
            ILogger? logger
        ) {
            _options = options ?? new SnapshotTagValuePushOptions();
            _maxSubscriptionCount = _options.MaxSubscriptionCount;
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            BackgroundTaskService.QueueBackgroundWorkItem(ProcessTagSubscriptionChangesChannel, DisposedToken);
            BackgroundTaskService.QueueBackgroundWorkItem(ProcessValueChangesChannel, DisposedToken);
        }


        /// <summary>
        /// Gets the <see cref="TagIdentifier"/> objects that corresponds to the specified tag names or IDs.
        /// </summary>
        /// <param name="context">
        ///   The call context for the caller.
        /// </param>
        /// <param name="tags">
        ///   The tag IDs or names.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the <see cref="TagIdentifier"/> objects for the tags. If a tag does 
        ///   not exist, or the caller is not authorized to access the tag, no entry should be returned 
        ///   for that tag.
        /// </returns>
        /// <remarks>
        ///   If the <see cref="SnapshotTagValuePushOptions"/> for the manager does not 
        ///   specify a <see cref="SnapshotTagValuePushOptions.TagResolver"/> callback, a 
        ///   <see cref="TagIdentifier"/> will be returned for each entry in the <paramref name="tags"/> 
        ///   list using the entry as the tag ID and name.
        /// </remarks>
        protected virtual async ValueTask<IEnumerable<TagIdentifier>> ResolveTags(IAdapterCallContext context, IEnumerable<string> tags, CancellationToken cancellationToken) {
            return _options.TagResolver == null
                ? tags.Select(tag => new TagIdentifier(tag, tag))
                : await _options.TagResolver.Invoke(context, tags, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the composite set of tags that are currently being subscribed to by all 
        /// subscribers.
        /// </summary>
        /// <returns>
        ///   The subscribed tags.
        /// </returns>
        protected IEnumerable<TagIdentifier> GetSubscribedTags() {
            _subscriptionsLock.EnterReadLock();
            try {
                return _subscriberCount.Keys.ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Called when topics are added to a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="topics">
        ///   The subscription topics
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the operation.
        /// </returns>
        private async Task OnTopicsAddedToSubscription(TagValueSubscriptionChannel<int> subscription, IEnumerable<TagIdentifier> topics, CancellationToken cancellationToken) {
            subscription.AddTopics(topics);

            foreach (var topic in topics) {
                if (_currentValueByTagId.TryGetValue(topic.Id, out var value)) {
                    subscription.Publish(value, true);
                }
            }

            TaskCompletionSource<bool> processed = null!;

            _subscriptionsLock.EnterWriteLock();
            try {
                var newSubscriptions = new List<TagIdentifier>();

                foreach (var topic in topics) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                        subscriberCount = 0;
                        newSubscriptions.Add(topic);
                    }

                    _subscriberCount[topic] = ++subscriberCount;
                }

                if (newSubscriptions.Count > 0) {
                    processed = new TaskCompletionSource<bool>();
                    _topicSubscriptionChangesChannel.Writer.TryWrite((newSubscriptions, true, processed));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (processed == null) {
                return;
            }

            // Wait for last change to be processed.
            await processed.Task.WithCancellation(DisposedToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Called when topics are removed from a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="topics">.
        ///   The subscription topics
        /// </param>
        /// <returns>
        ///   A task that will process the operation.
        /// </returns>
        private void OnTagsRemovedFromSubscription(TagValueSubscriptionChannel<int> subscription, IEnumerable<TagIdentifier> topics) {
            _subscriptionsLock.EnterWriteLock();
            try {
                var removedSubscriptions = new List<TagIdentifier>();

                foreach (var topic in topics) {
                    if (topic == null || !subscription.RemoveTopic(topic)) {
                        continue;
                    }

                    if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                        continue;
                    }

                    --subscriberCount;

                    if (subscriberCount == 0) {
                        _subscriberCount.Remove(topic);
                        removedSubscriptions.Add(topic);
                    }
                    else {
                        _subscriberCount[topic] = subscriberCount;
                    }
                }

                if (removedSubscriptions.Count > 0) {
                    _topicSubscriptionChangesChannel.Writer.TryWrite((removedSubscriptions, false, null!));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="initialTopics">
        ///   The initial topics to subscribe to.
        /// </param>
        private async Task OnSubscriptionAddedInternal(TagValueSubscriptionChannel<int> subscription, IEnumerable<string> initialTopics) {
            var initialTags = initialTopics.Any()
                ? await ResolveTags(subscription.Context, initialTopics, subscription.CancellationToken).ConfigureAwait(false)
                : Array.Empty<TagIdentifier>();
            initialTags = initialTags.Where(x => x != null).ToArray();
            await OnTopicsAddedToSubscription(subscription, initialTags, subscription.CancellationToken).ConfigureAwait(false);
            OnSubscriptionAdded(subscription);
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

            try {
                OnTagsRemovedFromSubscription(subscription, subscription.Topics);
            }
            finally {
                subscription.Dispose();
                OnSubscriptionCancelled(subscription);
            }
        }


        /// <summary>
        /// Called when the number of subscribers for a tag changes from zero to one.
        /// </summary>
        /// <param name="tags">
        ///   The tags that have been added.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the change.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tags"/> is <see langword="null"/>.
        /// </exception>
        protected virtual Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            return _options.OnTagSubscriptionsAdded == null
                ? Task.CompletedTask
                : _options.OnTagSubscriptionsAdded.Invoke(tags, cancellationToken);
        }


        /// <summary>
        /// Called when the number of subscribers for a tag changes from one to zero.
        /// </summary>
        /// <param name="tags">
        ///   The tags that have been removed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the change.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tags"/> is <see langword="null"/>.
        /// </exception>
        protected virtual async Task OnTagsRemoved(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            if (_options.OnTagSubscriptionsRemoved != null) {
                await _options.OnTagSubscriptionsRemoved.Invoke(tags, cancellationToken).ConfigureAwait(false);
            }

            // Remove current value if we are caching it.
            foreach (var tag in tags) {
                _currentValueByTagId.TryRemove(tag.Id, out var _);
            }
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
                try {
                    if (!await _topicSubscriptionChangesChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        break;
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }

                while (_topicSubscriptionChangesChannel.Reader.TryRead(out var change)) {
                    try {
                        if (change.Added) {
                            await OnTagsAdded(change.Tags, cancellationToken).ConfigureAwait(false);
                        }
                        else {
                            await OnTagsRemoved(change.Tags, cancellationToken).ConfigureAwait(false);
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
                            change.Tags.Count,
                            change.Added
                                ? SubscriptionUpdateAction.Subscribe
                                : SubscriptionUpdateAction.Unsubscribe
                        );
                    }
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
                try {
                    if (!await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        break;
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }

                while (_masterChannel.Reader.TryRead(out var item)) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
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
        }


        /// <summary>
        /// Runs a long-running task that will process changes on a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="channel">
        ///   The subscription changes channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The long-running task.
        /// </returns>
        private async Task RunSubscriptionChanngesListener(TagValueSubscriptionChannel<int> subscription, ChannelReader<TagValueSubscriptionUpdate> channel, CancellationToken cancellationToken) {
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item?.Tags == null) {
                        continue;
                    }

                    var topics = item.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    if (topics.Length == 0) {
                        continue;
                    }

                    var tags = await ResolveTags(subscription.Context, topics, cancellationToken).ConfigureAwait(false);
                    tags = tags.Where(x => x != null).ToArray();
                    if (!tags.Any()) {
                        continue;
                    }

                    if (item.Action == SubscriptionUpdateAction.Subscribe) {
                        await OnTopicsAddedToSubscription(subscription, tags, cancellationToken).ConfigureAwait(false);
                    }
                    else {
                        OnTagsRemovedFromSubscription(subscription, tags);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public async Task<ChannelReader<TagValueQueryResult>> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            ChannelReader<TagValueSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            ValidationExtensions.ValidateObject(request);
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (_maxSubscriptionCount > 0 && _subscriptions.Count >= _maxSubscriptionCount) {
                throw new InvalidOperationException(Resources.Error_TooManySubscriptions);
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = new TagValueSubscriptionChannel<int>(
                subscriptionId,
                context,
                BackgroundTaskService,
                request.PublishInterval,
                new[] { DisposedToken, cancellationToken },
                () => OnSubscriptionCancelledInternal(subscriptionId),
                10
            );
            _subscriptions[subscriptionId] = subscription;

            try {
                await OnSubscriptionAddedInternal(subscription, request.Tags?.Where(x => x != null)?.ToArray() ?? Array.Empty<string>()).ConfigureAwait(false);
            }
            catch {
                OnSubscriptionCancelledInternal(subscriptionId);
                throw;
            }

            BackgroundTaskService.QueueBackgroundWorkItem(ct => RunSubscriptionChanngesListener(subscription, channel, ct), subscription.CancellationToken);

            return subscription.Reader;
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        protected virtual void OnSubscriptionAdded(TagValueSubscriptionChannel<int> subscription) { }


        /// <summary>
        /// Invoked when a subscription is cancelled.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        protected virtual void OnSubscriptionCancelled(TagValueSubscriptionChannel<int> subscription) { }


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
            var subscribers = _subscriptions.Values.Where(x => x.Topics.Any(x => IsTopicMatch(x, tagIdentifier))).ToArray();

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


        /// <summary>
        /// Checks to see if the specified subscription topic and tag value topic match.
        /// </summary>
        /// <param name="subscriptionTopic">
        ///   The topic that was specified by the subscriber.
        /// </param>
        /// <param name="valueTopic">
        ///   The topic for the received tag value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value should be published to the subscriber, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   This method is used to determine if a tag value will be pushed to a subscriber. 
        ///   The default behaviour is to return <see langword="true"/> if <paramref name="subscriptionTopic"/> 
        ///   and <paramref name="valueTopic"/> have matching tag IDs. If a <see cref="SnapshotTagValuePushOptions.IsTopicMatch"/> 
        ///   delegate is provided in the feature options, this delegate will be invoked if the 
        ///   default check returns <see langword="false"/>.
        /// </para>
        /// 
        /// <para>
        ///   Override this method if your adapter allows e.g. the use of wildcards in 
        ///   subscription topics.
        /// </para>
        /// 
        /// </remarks>
        protected virtual bool IsTopicMatch(TagIdentifier subscriptionTopic, TagIdentifier valueTopic) {
            if (TagIdentifierComparer.Id.Equals(subscriptionTopic, valueTopic)) {
                return true;
            }

            if (_options.IsTopicMatch != null) {
                return _options.IsTopicMatch(subscriptionTopic, valueTopic);
            }

            return false;
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
        public string AdapterId { get; set; } = default!;

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
        public Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, ValueTask<IEnumerable<TagIdentifier>>>? TagResolver { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a tag changes from zero 
        /// to one.
        /// </summary>
        public Func<IEnumerable<TagIdentifier>, CancellationToken, Task>? OnTagSubscriptionsAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a tag changes from one 
        /// to zero.
        /// </summary>
        public Func<IEnumerable<TagIdentifier>, CancellationToken, Task>? OnTagSubscriptionsRemoved { get; set; }

        /// <summary>
        /// A delegate that is invoked to determine if the topic for a subscription matches the 
        /// topic for a received tag value.
        /// </summary>
        /// <remarks>
        ///   The first parameter passed to the delegate is the subscription topic, and the second 
        ///   parameter is the topic for the received tag value.
        /// </remarks>
        public Func<TagIdentifier, TagIdentifier, bool>? IsTopicMatch { get; set; }


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
        public static Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, ValueTask<IEnumerable<TagIdentifier>>> CreateTagResolver(ITagInfo feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return async (context, tags, cancellationToken) => {
                var ch = await feature.GetTags(context, new GetTagsRequest() {
                    Tags = tags?.ToArray()!
                }, cancellationToken).ConfigureAwait(false);

                return await ch.ToEnumerable(tags.Count(), cancellationToken).ConfigureAwait(false);
            };
        }

    }
}
