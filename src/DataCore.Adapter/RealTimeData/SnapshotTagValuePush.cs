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
    /// <see cref="ISnapshotTagValuePush"/> implementation.
    /// </summary>
    public class SnapshotTagValuePush : SubscriptionManager<SnapshotTagValuePushOptions, TagIdentifier, TagValueQueryResult, TagValueSubscriptionChannel>, ISnapshotTagValuePush {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Holds the current values for subscribed tags.
        /// </summary>
        private readonly ConcurrentDictionary<TagIdentifier, TagValueQueryResult> _currentValueCache = new ConcurrentDictionary<TagIdentifier, TagValueQueryResult>();

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
        public SnapshotTagValuePush(SnapshotTagValuePushOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger) 
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
        public static Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, IAsyncEnumerable<TagIdentifier>> CreateTagResolverFromFeature(ITagInfo feature) {
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
        public static Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, IAsyncEnumerable<TagIdentifier>> CreateTagResolverFromAdapter(IAdapter adapter) {
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
                var subscription = CreateSubscription<ISnapshotTagValuePush>(context, nameof(Subscribe), request, ctSource.Token);
                if (request.Tags != null && request.Tags.Any()) {
                    await OnTagSubscriptionChanges(
                        subscription, 
                        SubscriptionUpdateAction.Subscribe, 
                        ResolveTags(context, request.Tags, cancellationToken), 
                        cancellationToken
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
            Action cleanup, 
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
            lock (_subscriberCount) { 
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
        ///   The default behaviour of <see cref="HasSubscribers"/> is to check if at least one 
        ///   subscription is observing the <paramref name="tag"/>. If this check fails, a call 
        ///   to <see cref="SnapshotTagValuePushOptions.HasSubscribers"/> is performed, if a 
        ///   callback is provided for this property in the options passed to the 
        ///   <see cref="SnapshotTagValuePush"/>.
        /// </remarks>
        protected virtual bool HasSubscribers(TagIdentifier tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            lock (_subscriberCount) {
                if (_subscriberCount.TryGetValue(tag, out var count) && count > 0) {
                    return true;
                }
            }

            if (Options.HasSubscribers != null) {
                return Options.HasSubscribers.Invoke(tag);
            } 

            return false;
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

            //// Reject values with no subscribers
            //if (!HasSubscribers(tagIdentifier)) {
            //    return false;
            //}

            // Add the value
            var latestValue = _currentValueCache.AddOrUpdate(
                tagIdentifier,
                message,
                (key, prev) => prev.Value.UtcSampleTime > message.Value.UtcSampleTime
                    ? prev
                    : message
            );

            if (latestValue != message) {
                // There was already a later value sent for this tag.
                return false;
            }

            return await base.ValueReceived(message, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Tries to retrieve the cached value for the specified tag.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="value">
        ///   The cached tag value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if a cached value was found for the <paramref name="tag"/>, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public bool TryGetCachedValue(TagIdentifier tag, out TagValueQueryResult? value) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            return _currentValueCache.TryGetValue(tag, out value);
        }


        /// <summary>
        /// Removes all tag values from the cache that no longer have any subscribers.
        /// </summary>
        public void RemoveStaleCachedValues() {
            foreach (var item in _currentValueCache.Keys) {
                RemoveCachedValueIfNoSubscribers(item);
            }
        }


        /// <summary>
        /// Removes the cached value for the specified tag if it has no subscribers.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        private void RemoveCachedValueIfNoSubscribers(TagIdentifier tag) {
            // Lock around _subscriberCount here so that it is not possible for a tag to gain
            // subscribers between calling HasSubscribers and removing the cached value.
            //
            // HasSubscribers is virtual, but the base implementation uses _subscriberCount to
            // synchronise access.
            lock (_subscriberCount) {
                if (!HasSubscribers(tag)) {
                    TryRemoveCachedValue(tag);
                }
            }
        }


        /// <summary>
        /// Removes the cached value for the specified tag.
        /// </summary>
        /// <param name="tag">
        ///   The identifier for the tag to remove from the cache.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if a value for the specified <paramref name="tag"/> was 
        ///   removed, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public bool TryRemoveCachedValue(TagIdentifier tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            return _currentValueCache.TryRemove(tag, out _);
        }


        /// <summary>
        /// Removes all cached tag values.
        /// </summary>
        public void RemoveAllCachedValues() {
            _currentValueCache.Clear();
        }


        /// <inheritdoc/>
        protected override IDictionary<string, string> GetHealthCheckProperties(IAdapterCallContext context) {
            var result = base.GetHealthCheckProperties(context);

            int subscribedTagCount;
            
            lock (_subscriberCount) {
                subscribedTagCount = _subscriberCount.Count;
            }

            result[Resources.HealthChecks_Data_TagCount] = subscribedTagCount.ToString(context?.CultureInfo);

            return result;
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
                    OnTagsRemovedFromSubscription(subscription, tags);
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
                if (_currentValueCache.TryGetValue(topic, out var value)) {
                    subscription.Publish(value, true);
                }
            }

            TaskCompletionSource<bool> processed = null!;

            lock(_subscriberCount) {
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
        private void OnTagsRemovedFromSubscription(TagValueSubscriptionChannel subscription, IEnumerable<TagIdentifier> topics) {
            lock(_subscriberCount) {
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
                : Options.OnTagSubscriptionsAdded.Invoke(tags, cancellationToken);
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
                await Options.OnTagSubscriptionsRemoved.Invoke(tags, cancellationToken).ConfigureAwait(false);
            }

            //foreach (var tag in tags) {
            //    RemoveCachedValueIfNoSubscribers(tag);
            //}
        }


        /// <inheritdoc/>
        protected override void OnSubscriptionCancelled(TagValueSubscriptionChannel subscription) {
            base.OnSubscriptionCancelled(subscription);
            if (subscription != null) {
                OnTagsRemovedFromSubscription(subscription, subscription.Topics);
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

                        Logger.LogError(
                            e,
                            Resources.Log_ErrorWhileProcessingSnapshotSubscriptionChange,
                            change.Topics.Count,
                            change.Added
                                ? SubscriptionUpdateAction.Subscribe
                                : SubscriptionUpdateAction.Unsubscribe
                        );
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
                lock (_subscriberCount) {
                    _subscriberCount.Clear();
                }
            }

            _isDisposed = true;
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValuePush"/>.
    /// </summary>
    public class SnapshotTagValuePushOptions : SubscriptionManagerOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/>.
        /// </summary>
        /// <remarks>
        ///   <see cref="SnapshotTagValuePush.CreateTagResolverFromAdapter(IAdapter)"/> or 
        ///   <see cref="SnapshotTagValuePush.CreateTagResolverFromFeature(ITagInfo)"/> can be 
        ///   used to generate a compatible delegate using an existing adapter or 
        ///   <see cref="ITagInfo"/> implementation.
        /// </remarks>
        public Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, IAsyncEnumerable<TagIdentifier>>? TagResolver { get; set; }

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
        /// <para>
        ///   The first parameter passed to the delegate is the subscription topic, and the second 
        ///   parameter is the topic for the received tag value.
        /// </para>
        /// <para>
        ///   Note that specifying a value for this property overrides the default <see cref="SnapshotTagValuePush.IsTopicMatch"/> 
        ///   behaviour, which checks to see if the tag ID for the incoming value exactly matches 
        ///   the tag ID on a subscribed topic.
        /// </para>
        /// </remarks>
        public Func<TagIdentifier, TagIdentifier, CancellationToken, ValueTask<bool>>? IsTopicMatch { get; set; }

        /// <summary>
        /// A delegate that is invoked to determine if a given tag has subscribers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It is generally not required to specify a value for this property. An example of when 
        /// it would be desirable to provide a delegate would be if you wanted to allow wildcard 
        /// subscriptions, as the default implementation of <see cref="SnapshotTagValuePush.HasSubscribers"/> 
        /// only checks to see if there is a subscription that exactly matches a given 
        /// <see cref="TagIdentifier"/>.
        /// </para>
        /// </remarks>
        public Func<TagIdentifier, bool>? HasSubscribers { get; set; }

    }

}
