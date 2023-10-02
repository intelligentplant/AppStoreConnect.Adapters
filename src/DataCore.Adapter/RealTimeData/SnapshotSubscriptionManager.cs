using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Subscriptions;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// <see cref="ISnapshotTagValuePush"/> implementation based on <see cref="SubscriptionManager{T}"/>.
    /// </summary>
    public class SnapshotSubscriptionManager : SubscriptionManager<TagValueQueryResult>, ISnapshotTagValuePush {

        private bool _disposed;

        private readonly SnapshotSubscriptionManagerOptions? _options;

        IBackgroundTaskService IBackgroundTaskServiceProvider.BackgroundTaskService => BackgroundTaskService;


        public SnapshotSubscriptionManager(
            SnapshotSubscriptionManagerOptions? options,
            IBackgroundTaskService? backgroundTaskService,
            ILogger? logger = null
        ) : base(x => x.TagName, options, backgroundTaskService, logger) {
            _options = options;
        }


        /// <summary>
        /// Creates a delegate compatible with <see cref="SnapshotSubscriptionManagerOptions.TagResolver"/> using an 
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
        /// Creates a delegate compatible with <see cref="SnapshotSubscriptionManagerOptions.TagResolver"/> using an 
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


        protected override string GetTopic(TagValueQueryResult message) {
            return _options?.GetTopic?.Invoke(message) ?? base.GetTopic(message);
        }


        protected override async ValueTask OnFirstSubscriberAddedAsync(IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            if (_options?.OnFirstSubscriberAdded != null) {
                await _options.OnFirstSubscriberAdded(topics, cancellationToken).ConfigureAwait(false);
            }
        }


        protected override async ValueTask OnLastSubscriberRemovedAsync(IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            if (_options?.OnLastSubscriberRemoved != null) {
                await _options.OnLastSubscriberRemoved(topics, cancellationToken).ConfigureAwait(false);
            }
        }


        async IAsyncEnumerable<TagValueQueryResult> ISnapshotTagValuePush.Subscribe(
            IAdapterCallContext context, 
            CreateSnapshotTagValueSubscriptionRequest request, 
            IAsyncEnumerable<TagValueSubscriptionUpdate> subscriptionUpdates,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            ValidationExtensions.ValidateObject(request);
            if (subscriptionUpdates == null) {
                throw new ArgumentNullException(nameof(subscriptionUpdates));
            }

            using (var subscription = CreateSubscription(context.CorrelationId)) {
                foreach (var tag in await ResolveTagsAsync(context, request.Tags, cancellationToken).ConfigureAwait(false)) {
                    await subscription.SubscribeAsync(tag, cancellationToken).ConfigureAwait(false);
                }

                BackgroundTaskService.QueueBackgroundWorkItem(ct => ProcessSubscriptionChangesAsync(context, subscription, subscriptionUpdates, ct), cancellationToken);

                if (request.PublishInterval <= TimeSpan.Zero) {
                    await foreach (var item in subscription.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                        yield return item;
                    }
                }
                else {
                    using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, LifetimeToken)) {
                        var values = new Dictionary<string, TagValueQueryResult>(StringComparer.OrdinalIgnoreCase);

                        BackgroundTaskService.QueueBackgroundWorkItem(ct => RunSubscriptionWithPublishIntervalAsync(subscription, values, ct), ctSource.Token);

                        do {
                            await Task.Delay(request.PublishInterval, ctSource.Token).ConfigureAwait(false);

                            TagValueQueryResult[] valuesToEmit;

                            lock (values) {
                                if (values.Count == 0) {
                                    valuesToEmit = Array.Empty<TagValueQueryResult>();
                                    continue;
                                }

                                valuesToEmit = values.Values.ToArray();
                            }

                            foreach (var item in valuesToEmit) {
                                if (ctSource.IsCancellationRequested) {
                                    break;
                                }
                                yield return item;
                            }
                        } while (!ctSource.IsCancellationRequested);
                    }
                }
            }
        }


        private async Task<IEnumerable<string>> ResolveTagsAsync(
            IAdapterCallContext context, 
            IEnumerable<string> tags, 
            CancellationToken cancellationToken
        ) {
            if (_options?.TagResolver == null) {
                return tags;
            }

            var tagsToSubscribe = new List<string>();
            var tagsToVerify = new List<string>();

            foreach (var tag in tags) {
                var topic = new SubscriptionTopic(tag, _options);
                if (topic.TopicContainsWildcard) {
                    tagsToSubscribe.Add(topic.Topic);
                }
                else {
                    tagsToVerify.Add(topic.Topic);
                }
            }

            if (_options?.TagResolver != null) {
                await foreach (var item in _options.TagResolver(context, tagsToVerify, cancellationToken).ConfigureAwait(false)) {
                    tagsToSubscribe.Add(item.Name);
                }
            }

            return tagsToSubscribe;
        }


        private async Task ProcessSubscriptionChangesAsync(
            IAdapterCallContext context,
            Subscription<TagValueQueryResult> subscription, 
            IAsyncEnumerable<TagValueSubscriptionUpdate> subscriptionUpdates, 
            CancellationToken cancellationToken
        ) {
            await foreach (var item in subscriptionUpdates.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                switch (item.Action) {
                    case Common.SubscriptionUpdateAction.Subscribe:
                        foreach (var tag in await ResolveTagsAsync(context, item.Tags, cancellationToken).ConfigureAwait(false)) {
                            await subscription.SubscribeAsync(tag, cancellationToken).ConfigureAwait(false);
                        }
                        break;
                    case Common.SubscriptionUpdateAction.Unsubscribe:
                        foreach (var tag in await ResolveTagsAsync(context, item.Tags, cancellationToken).ConfigureAwait(false)) {
                            await subscription.UnsubscribeAsync(tag, cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }
            }
        }


        private async Task RunSubscriptionWithPublishIntervalAsync(Subscription<TagValueQueryResult> subscription, Dictionary<string, TagValueQueryResult> values, CancellationToken cancellationToken) { 
            await foreach (var item in subscription.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                lock (values) {
                    values[item.TagName] = item;
                }
            }
        }

    }
}
