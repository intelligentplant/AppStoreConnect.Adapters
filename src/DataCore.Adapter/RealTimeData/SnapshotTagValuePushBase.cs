using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Base <see cref="ISnapshotTagValuePush"/> implementation.
    /// </summary>
    public abstract partial class SnapshotTagValuePushBase : SubscriptionManager<SnapshotTagValuePushOptions, TagIdentifier, TagValueQueryResult, TagValueSubscriptionChannel>, ISnapshotTagValuePush {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Channel that is used to publish changes to subscribed topics.
        /// </summary>
        private readonly Channel<(List<TagIdentifier> Topics, bool Added, TaskCompletionSource<bool> Processed)> _topicSubscriptionChangesChannel = Channel.CreateUnbounded<(List<TagIdentifier>, bool, TaskCompletionSource<bool>)>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// Maps from tag ID to the subscriber count for that tag.
        /// </summary>
        private readonly Dictionary<TagIdentifier, int> _subscriberCount = new Dictionary<TagIdentifier, int>(TagIdentifierComparer.Id);

        /// <summary>
        /// Lock for performing subscription modifications.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _subscriptionLock = new Nito.AsyncEx.AsyncReaderWriterLock();


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushBase"/> object.
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
        protected SnapshotTagValuePushBase(SnapshotTagValuePushOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger<SnapshotTagValuePushBase>? logger)
            : base(options, backgroundTaskService, logger) {
            BackgroundTaskService.QueueBackgroundWorkItem(
                ProcessTagSubscriptionChangesChannel,
                DisposedToken
            );
        }


        /// <summary>
        /// Creates a delegate compatible with <see cref="SnapshotTagValuePushOptions.TagResolver"/> using an 
        /// <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="ITagInfo"/> feature to use.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static TagResolver CreateTagResolverFromFeature(ITagInfo feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return (context, tags, cancellationToken) => feature.GetTags(context, new GetTagsRequest() {
                Tags = tags?.ToArray()!
            }, cancellationToken);
        }


        /// <summary>
        /// Creates a delegate compatible with <see cref="SnapshotTagValuePushOptions.TagResolver"/> using an 
        /// <see cref="IAdapter"/> that implements the <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="adapter">
        ///   The <see cref="IAdapter"/> to use to resolve tags.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        /// <remarks>
        ///   The adapter's <see cref="ITagInfo"/> feature will be resolved every time the resulting 
        ///   delegate is invoked. If the feature cannot be resolved, the delegate will return an 
        ///   empty <see cref="IAsyncEnumerable{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static TagResolver CreateTagResolverFromAdapter(IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            return (context, tags, cancellationToken) => adapter.TryGetFeature<ITagInfo>(out var feature)
                ? feature!.GetTags(context, new GetTagsRequest() { Tags = tags?.ToArray()! }, cancellationToken)
                : Array.Empty<TagIdentifier>().PublishToChannel().ReadAllAsync(cancellationToken);
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagValueQueryResult> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
            [EnumeratorCancellation]
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

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposedToken)) {
                var subscription = await CreateSubscriptionAsync<ISnapshotTagValuePush>(context, nameof(Subscribe), request, ctSource.Token).ConfigureAwait(false);
                if (request.Tags != null && request.Tags.Any()) {
                    await OnTagSubscriptionChanges(
                        subscription,
                        SubscriptionUpdateAction.Subscribe,
                        ResolveTags(context, request.Tags, ctSource.Token),
                        ctSource.Token
                    ).ConfigureAwait(false);
                }

                BackgroundTaskService.QueueBackgroundWorkItem(
                    ct => RunSubscriptionChangesListener(subscription, channel, ct),
                    null,
                    true,
                    subscription.CancellationToken,
                    ctSource.Token
                );

                await foreach (var item in subscription.ReadAllAsync(ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }


        /// <inheritdoc/>
        protected override TagValueSubscriptionChannel CreateSubscriptionChannel(
            IAdapterCallContext context,
            int id,
            int channelCapacity,
            CancellationToken[] cancellationTokens,
            Func<ValueTask> cleanup,
            object? state
        ) {
            var request = (CreateSnapshotTagValueSubscriptionRequest) state!;
            return new TagValueSubscriptionChannel(
                id,
                context,
                BackgroundTaskService,
                request?.PublishInterval ?? TimeSpan.Zero,
                cancellationTokens,
                cleanup,
                channelCapacity
            );
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the <see cref="TagIdentifier"/> 
        ///   objects for the tags. If a tag does not exist, or the caller is not authorized to 
        ///   access the tag, no entry should be returned for that tag.
        /// </returns>
        /// <remarks>
        ///   If the <see cref="SnapshotTagValuePushOptions"/> for the manager does not 
        ///   specify a <see cref="SnapshotTagValuePushOptions.TagResolver"/> callback, a 
        ///   <see cref="TagIdentifier"/> will be returned for each entry in the <paramref name="tags"/> 
        ///   list using the entry as the tag ID and name.
        /// </remarks>
        protected virtual async IAsyncEnumerable<TagIdentifier> ResolveTags(
            IAdapterCallContext context,
            IEnumerable<string> tags,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (Options.TagResolver == null) {
                foreach (var tag in tags) {
                    yield return new TagIdentifier(tag, tag);
                }
                yield break;
            }

            await foreach (var item in Options.TagResolver.Invoke(context, tags, cancellationToken).ConfigureAwait(false)) {
                if (item == null) {
                    continue;
                }
                yield return item;
            }
        }


        /// <summary>
        /// Gets the composite set of tags that are currently being subscribed to by all 
        /// subscribers.
        /// </summary>
        /// <returns>
        ///   The subscribed tags.
        /// </returns>
        public IEnumerable<TagIdentifier> GetSubscribedTags() {
            using (_subscriptionLock.ReaderLock()) {
                return _subscriberCount.Keys.ToArray();
            }
        }


        /// <summary>
        /// Tests if the specified tag has subscribers.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag has subscribers, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The default behaviour of <see cref="HasSubscribersAsync"/> is to check if at least 
        ///   one subscription is observing the <paramref name="tag"/>. If this check fails, a call 
        ///   to <see cref="SnapshotTagValuePushOptions.HasSubscribers"/> is performed, if a 
        ///   callback is provided for this property in the options passed to the 
        ///   <see cref="SnapshotTagValuePush"/>.
        /// </remarks>
        protected async ValueTask<bool> HasSubscribersAsync(TagIdentifier tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            using (await _subscriptionLock.ReaderLockAsync().ConfigureAwait(false)) {
                return HasSubscribersCore(tag);
            }
            
        }


        /// <summary>
        /// Tests if the specified tag has subscribers.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag has subscribers, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <returns>
        ///   <see langword="true"/> if the tag has subscribers, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        private bool HasSubscribersCore(TagIdentifier tag) {
            if (_subscriberCount.TryGetValue(tag, out var count) && count > 0) {
                return true;
            }

            return Options.HasSubscribers != null
                ? Options.HasSubscribers.Invoke(tag)
                : false;
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> IsTopicMatch(TagValueQueryResult value, IEnumerable<TagIdentifier> topics, CancellationToken cancellationToken) {
            if (value == null) {
                return false;
            }
            var tagIdentifier = new TagIdentifier(value.TagId, value.TagName);

            // If a custom delegate has been specified, defer to that.
            if (Options.IsTopicMatch != null) {
                foreach (var topic in topics) {
                    if (await Options.IsTopicMatch(topic, tagIdentifier, cancellationToken).ConfigureAwait(false)) {
                        return true;
                    }
                }
                return false;
            }

            if (topics.Any(x => TagIdentifierComparer.Id.Equals(tagIdentifier, x))) {
                return true;
            }

            return false;
        }


        /// <inheritdoc/>
        public override async ValueTask<bool> ValueReceived(TagValueQueryResult message, CancellationToken cancellationToken = default) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var tagIdentifier = new TagIdentifier(message.TagId, message.TagName);

            // Add the value to the cache.
            if (!await AddOrUpdateCachedValueAsync(tagIdentifier, message, cancellationToken).ConfigureAwait(false)) {
                // The value was rejected.
                return false;
            }

            return await PublishValueToSubscribersAsync(message, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Publishes a value to subscribers without adding the value to the cache.
        /// </summary>
        /// <param name="value">
        ///   The value to publish.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="SnapshotTagValuePushBase"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        protected async ValueTask<bool> PublishValueToSubscribersAsync(TagValueQueryResult value, CancellationToken cancellationToken = default) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            return await base.ValueReceived(value, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override IEnumerable<KeyValuePair<string, string>> GetFeatureHealthCheckData(IAdapterCallContext context) {
            foreach (var item in base.GetFeatureHealthCheckData(context)) {
                yield return item;
            }

            int subscribedTagCount;

            using (_subscriptionLock.ReaderLock()) {
                subscribedTagCount = _subscriberCount.Count;
            }

            yield return new KeyValuePair<string, string>(Resources.HealthChecks_Data_TagCount, subscribedTagCount.ToString(context?.CultureInfo));
        }


        /// <summary>
        /// Called when tags are added to or removed from a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="action">
        ///   The subscription change type.
        /// </param>
        /// <param name="resolvedTags">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit the tags that were added or 
        ///   removed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will process the operation.
        /// </returns>
        private async Task OnTagSubscriptionChanges(
            TagValueSubscriptionChannel subscription,
            SubscriptionUpdateAction action,
            IAsyncEnumerable<TagIdentifier> resolvedTags,
            CancellationToken cancellationToken
        ) {
            // We will iterate over the resolved tags enumerable and then call OnTagsAddedToSubscription
            // or OnTagsRemovedFromSubscription using batch sizes of up to 100 tags.
            const int batchSize = 100;
            var tags = new List<TagIdentifier>(batchSize);

            async Task ProcessBatch() {
                if (action == SubscriptionUpdateAction.Subscribe) {
                    await OnTagsAddedToSubscription(subscription, tags, cancellationToken).ConfigureAwait(false);
                }
                else {
                    await OnTagsRemovedFromSubscription(subscription, tags, cancellationToken).ConfigureAwait(false);
                }
                tags.Clear();
            }

            await foreach (var tag in resolvedTags.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                if (tag == null) {
                    continue;
                }

                tags.Add(tag);
                if (tags.Count == batchSize) {
                    await ProcessBatch().ConfigureAwait(false);
                }
            }

            if (tags.Count > 0) {
                await ProcessBatch().ConfigureAwait(false);
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
        private async Task OnTagsAddedToSubscription(TagValueSubscriptionChannel subscription, IEnumerable<TagIdentifier> topics, CancellationToken cancellationToken) {
            subscription.AddTopics(topics);

            foreach (var topic in topics) {
                if (topic == null) {
                    continue;
                }
                var value = await GetCachedValueAsync(topic, cancellationToken).ConfigureAwait(false);
                if (value != null) {
                    subscription.Publish(value, true);
                }
            }

            TaskCompletionSource<bool> processed = null!;

            using (await _subscriptionLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the operation.
        /// </returns>
        private async Task OnTagsRemovedFromSubscription(TagValueSubscriptionChannel subscription, IEnumerable<TagIdentifier> topics, CancellationToken cancellationToken) {
            using (await _subscriptionLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
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

            return Options.OnTagSubscriptionsAdded == null
                ? Task.CompletedTask
                : Options.OnTagSubscriptionsAdded.Invoke(this, tags, cancellationToken);
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

            if (Options.OnTagSubscriptionsRemoved != null) {
                await Options.OnTagSubscriptionsRemoved.Invoke(this, tags, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask OnSubscriptionCancelledAsync(TagValueSubscriptionChannel subscription, CancellationToken cancellationToken) {
            await base.OnSubscriptionCancelledAsync(subscription, cancellationToken).ConfigureAwait(false);
            if (subscription != null) {
                await OnTagsRemovedFromSubscription(subscription, subscription.Topics, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Removes the cached values for the specified tags if they have no subscribers.
        /// </summary>
        /// <param name="tags">
        ///   The tags.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will perform the removal if required.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tags"/> is <see langword="null"/>.
        /// </exception>
        protected async ValueTask RemoveCachedValuesIfNoSubscribersAsync(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken = default) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            using (await _subscriptionLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                foreach (var tag in tags) {
                    if (tag == null) {
                        continue;
                    }

                    if (!HasSubscribersCore(tag)) {
                        await RemoveCachedValueAsync(tag, cancellationToken).ConfigureAwait(false);
                    }
                }
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
        private async Task ProcessTagSubscriptionChangesChannel(CancellationToken cancellationToken) {
            using var loggerScope = BeginLoggerScope();

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
                            await OnTagsAdded(change.Topics, cancellationToken).ConfigureAwait(false);
                        }
                        else {
                            await OnTagsRemoved(change.Topics, cancellationToken).ConfigureAwait(false);
                        }

                        if (change.Processed != null) {
                            change.Processed.TrySetResult(true);
                        }
                    }
                    catch (Exception e) {
                        if (change.Processed != null) {
                            change.Processed.TrySetException(e);
                        }

                        LogSubscriptionChangeError(Logger, e, change.Topics.Count, change.Added ? SubscriptionUpdateAction.Subscribe : SubscriptionUpdateAction.Unsubscribe);
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
        private async Task RunSubscriptionChangesListener(TagValueSubscriptionChannel subscription, IAsyncEnumerable<TagValueSubscriptionUpdate> channel, CancellationToken cancellationToken) {
            await foreach (var item in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                cancellationToken.ThrowIfCancellationRequested();
                if (item?.Tags == null) {
                    continue;
                }

                var topics = item.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                if (topics.Length == 0) {
                    continue;
                }

                await OnTagSubscriptionChanges(
                    subscription,
                    item.Action,
                    ResolveTags(subscription.Context, topics, cancellationToken),
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _topicSubscriptionChangesChannel.Writer.TryComplete();
                using (_subscriptionLock.WriterLock()) {
                    _subscriberCount.Clear();
                }
            }

            _isDisposed = true;
        }


        /// <summary>
        /// Adds or updates the cached value for a tag.
        /// </summary>
        /// <param name="tag">
        ///   The tag identifier.
        /// </param>
        /// <param name="value">
        ///   The new tag value.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that returns <see langword="true"/> if the cached 
        ///   value was successfully updated, or <see langword="false"/> if the new value was 
        ///   rejected (for example, if the new value is older than the current cached value).
        /// </returns>
        protected abstract ValueTask<bool> AddOrUpdateCachedValueAsync(TagIdentifier tag, TagValueQueryResult value, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the cached value for a tag.
        /// </summary>
        /// <param name="tag">
        ///   The tag identifier.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that returns the <see cref="TagValueQueryResult"/> 
        ///   for the tag if a cached value exists, or <see langword="null"/> otherwise.
        /// </returns>
        protected abstract ValueTask<TagValueQueryResult?> GetCachedValueAsync(TagIdentifier tag, CancellationToken cancellationToken);


        /// <summary>
        /// Removes the cached value for a tag.
        /// </summary>
        /// <param name="tag">
        ///   The tag identifier.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that returns <see langword="true"/> if the cached 
        ///   value was successfully updated, or <see langword="false"/> if the new value was 
        ///   rejected (for example, if the new value is older than the current cached value).
        /// </returns>
        /// <seealso cref="RemoveCachedValuesIfNoSubscribersAsync"/>
        protected abstract ValueTask<bool> RemoveCachedValueAsync(TagIdentifier tag, CancellationToken cancellationToken);


        [LoggerMessage(10, LogLevel.Error, "Error while processing a subscription change for {count} tags. Action: {action}")]
        static partial void LogSubscriptionChangeError(ILogger logger, Exception e, int count, SubscriptionUpdateAction action);

    }

}
