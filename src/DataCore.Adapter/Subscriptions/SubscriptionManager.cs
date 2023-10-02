using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Subscriptions {
    public partial class SubscriptionManager<T> : IDisposable {

        private bool _disposed;

        private readonly ILogger _logger;

        protected IBackgroundTaskService BackgroundTaskService { get; }

        private readonly CancellationTokenSource _lifetimeTokenSource = new CancellationTokenSource();

        internal CancellationToken LifetimeToken => _lifetimeTokenSource.Token;

        private readonly Dictionary<ulong, HashSet<Subscription<T>>> _noWildcardSubscriptionsByTopicHash = new Dictionary<ulong, HashSet<Subscription<T>>>();

        private readonly Dictionary<ulong, TopicHashMaskSubscriptions> _wildcardSubscriptionsByTopicHash = new Dictionary<ulong, TopicHashMaskSubscriptions>();

        private readonly Nito.AsyncEx.AsyncReaderWriterLock _subscriptionsLock = new Nito.AsyncEx.AsyncReaderWriterLock();

        private readonly Func<T, string> _topicSelector;

        private readonly Channel<T> _publishChannel;

        private readonly ConcurrentDictionary<string, (T Message, SubscriptionTopic Topic)> _messageCache = new ConcurrentDictionary<string, (T, SubscriptionTopic)>(StringComparer.OrdinalIgnoreCase);

        internal SubscriptionManagerOptions? Options { get; }


        public SubscriptionManager(
            Func<T, string> topicSelector,
            SubscriptionManagerOptions? options = null,
            IBackgroundTaskService? backgroundTaskService = null,
            ILogger? logger = null
        ) {
            _topicSelector = topicSelector ?? throw new ArgumentNullException(nameof(topicSelector));
            Options = options;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;

            var publishChannelCapacity = options?.PublishChannelCapacity ?? 10000;
            if (publishChannelCapacity > 0) {
                _publishChannel = Channel.CreateBounded<T>(new BoundedChannelOptions(publishChannelCapacity) {
                    AllowSynchronousContinuations = false,
                    FullMode = options?.PublishChannelFullMode ?? BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });
            }
            else {
                _publishChannel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = false,
                    SingleReader = true,
                    SingleWriter = false
                });
            }

            BackgroundTaskService.QueueBackgroundWorkItem(RunPublishLoopAsync, LifetimeToken);
        }


        public Subscription<T> CreateSubscription(string? id = null) {
            return new Subscription<T>(this, id);
        }


        /// <summary>
        /// Gets the topic for the specified message.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        /// <returns>
        ///   The topic for the message.
        /// </returns>
        /// <remarks>
        ///   
        /// <para>
        ///   The default implementation of <see cref="GetTopic(T)"/> uses the delegate passed to 
        ///   the <see cref="SubscriptionManager{T}"/> constructor to determine the topic for the 
        ///   message.
        /// </para>
        /// 
        /// <para>
        ///   Override this method if you require alternative logic for determining the topic for 
        ///   a message.
        /// </para>
        /// 
        /// </remarks>
        protected virtual string GetTopic(T message) {
            return _topicSelector.Invoke(message);
        }


        protected virtual ValueTask OnFirstSubscriberAddedAsync(IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            // No-op
            return default;
        }


        protected virtual ValueTask OnLastSubscriberRemovedAsync(IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            // No-op
            return default;
        }


        protected virtual ValueTask OnSubscriptionsAddedAsync(Subscription<T> consumer, IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            if (Options?.Retain ?? false) {
                // Check if we have an initial message available for any of the subscribed
                // non-wildcard topics.

                var snapshot = topics.Any(x => x.TopicContainsWildcard)
                    ? _messageCache.Values
                    : null;

                foreach (var topic in topics) {
                    if (topic.TopicContainsWildcard) {
                        continue;
                    }

                    if (_messageCache.TryGetValue(topic.Topic, out var cachedMessage)) {
                        consumer.TryPublishCore(cachedMessage.Message);
                    }
                }
            }

            return default;
        }


        protected virtual ValueTask OnSubscriptionsRemovedAsync(Subscription<T> consumer, IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            // No-op
            return default;
        }


        internal async ValueTask SubscribeAsync(Subscription<T> consumer, SubscriptionTopic topic, CancellationToken cancellationToken = default) {
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken, cancellationToken)) {
                var topicArr = new[] { topic };
                
                using (await _subscriptionsLock.WriterLockAsync(ctSource.Token).ConfigureAwait(false)) {
                    var isNewTopic = false;

                    if (topic.TopicContainsWildcard) {
                        if (!_wildcardSubscriptionsByTopicHash.TryGetValue(topic.TopicHash, out var subscriptions)) {
                            subscriptions = new TopicHashMaskSubscriptions();
                            _wildcardSubscriptionsByTopicHash[topic.TopicHash] = subscriptions;
                            isNewTopic = true;
                        }
                        subscriptions.AddSubscription(consumer, topic);
                    }
                    else {
                        if (!_noWildcardSubscriptionsByTopicHash.TryGetValue(topic.TopicHash, out var subscriptions)) {
                            subscriptions = new HashSet<Subscription<T>>();
                            _noWildcardSubscriptionsByTopicHash[topic.TopicHash] = subscriptions;
                            isNewTopic = true;
                        }
                        subscriptions.Add(consumer);
                    }

                    if (isNewTopic) {
                        await OnFirstSubscriberAddedAsync(topicArr, ctSource.Token).ConfigureAwait(false);
                    }
                }

                await OnSubscriptionsAddedAsync(consumer, topicArr, ctSource.Token).ConfigureAwait(false);
            }
        }


        internal async ValueTask UnsubscribeAsync(Subscription<T> consumer, SubscriptionTopic topic, CancellationToken cancellationToken = default) {
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken, cancellationToken)) {
                var topicArr = new[] { topic };

                using (await _subscriptionsLock.WriterLockAsync(ctSource.Token).ConfigureAwait(false)) {
                    if (UnsubscribeCore(consumer, topic)) {
                        await OnLastSubscriberRemovedAsync(topicArr, ctSource.Token).ConfigureAwait(false);
                    }
                }

                await OnSubscriptionsRemovedAsync(consumer, topicArr, ctSource.Token).ConfigureAwait(false);
            }
        }


        private bool UnsubscribeCore(Subscription<T> consumer, SubscriptionTopic topic) {
            var isAbandonedTopic = false;
            
            if (topic.TopicContainsWildcard) {
                if (_wildcardSubscriptionsByTopicHash.TryGetValue(topic.TopicHash, out var subscriptions)) {
                    subscriptions.RemoveSubscription(consumer, topic);
                    if (subscriptions.SubscriptionsByHashMask.Count == 0) {
                        _wildcardSubscriptionsByTopicHash.Remove(topic.TopicHash);
                        isAbandonedTopic = true;
                    }
                }
            }
            else {
                if (_noWildcardSubscriptionsByTopicHash.TryGetValue(topic.TopicHash, out var subscriptions)) {
                    subscriptions.Remove(consumer);
                    if (subscriptions.Count == 0) {
                        _noWildcardSubscriptionsByTopicHash.Remove(topic.TopicHash);
                        isAbandonedTopic = true;
                    }
                }
            }

            return isAbandonedTopic;
        }


        internal void OnDisposed(Subscription<T> consumer) {
            using (var flag = new ManualResetEventSlim()) {
                var topics = consumer.Topics.ToArray();

                BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    try {
                        var topicsWithNoSubscribers = new List<SubscriptionTopic>();

                        using (await _subscriptionsLock.WriterLockAsync(ct).ConfigureAwait(false)) {
                            foreach (var topic in topics) {
                                if (UnsubscribeCore(consumer, topic)) {
                                    topicsWithNoSubscribers.Add(topic);
                                }
                            }
                        }

                        if (topicsWithNoSubscribers.Count > 0) {
                            await OnLastSubscriberRemovedAsync(topicsWithNoSubscribers, ct).ConfigureAwait(false);
                        }

                        await OnSubscriptionsRemovedAsync(consumer, topics, ct).ConfigureAwait(false);
                    }
                    finally {
                        flag.Set();
                    }
                }, LifetimeToken);

                flag.Wait();
            }
        }



        private async Task RunPublishLoopAsync(CancellationToken cancellationToken) {
            while (await _publishChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (!cancellationToken.IsCancellationRequested && _publishChannel.Reader.TryRead(out var message)) {
                    var topic = GetTopic(message);
                    if (topic == null) {
                        LogTopicUnavailable(message);
                        continue;
                    }

                    var subscriptionTopic = new SubscriptionTopic(topic, Options);
                    var possibleSubscribers = new List<Subscription<T>>();

                    using (await _subscriptionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                        if (_noWildcardSubscriptionsByTopicHash.TryGetValue(subscriptionTopic.TopicHash, out var subscribers)) {
                            possibleSubscribers.AddRange(subscribers);
                        }

                        foreach (var wcs in _wildcardSubscriptionsByTopicHash) {
                            var subscriptionHash = wcs.Key;
                            var subscriptionsByHashMask = wcs.Value.SubscriptionsByHashMask;
                            foreach (var shm in subscriptionsByHashMask) {
                                var subscriptionHashMask = shm.Key;
                                if ((subscriptionTopic.TopicHash & subscriptionHashMask) == subscriptionHash) {
                                    possibleSubscribers.AddRange(shm.Value);
                                }
                            }
                        }
                    }

                    if (possibleSubscribers.Count == 0) {
                        continue;
                    }

                    foreach (var subscriber in possibleSubscribers) {
                        if (cancellationToken.IsCancellationRequested) {
                            break;
                        }

                        try {
                            await subscriber.PublishAsync(topic, message, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException e) when (e.CancellationToken == cancellationToken) {
                            break;
                        }
                        catch (Exception e) {
                            LogConsumerSendError(e, subscriber.Id);
                        }
                    }
                }
            }
        }


        public async ValueTask PublishAsync(T message, CancellationToken cancellationToken = default) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            await _publishChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _publishChannel.Writer.TryComplete();
            _lifetimeTokenSource.Cancel();
            _lifetimeTokenSource.Dispose();

            _disposed = true;
        }


        [LoggerMessage(1, LogLevel.Error, "Error publishing a value to consumer '{consumerId}.")]
        partial void LogConsumerSendError(Exception error, string consumerId);

        [LoggerMessage(2, LogLevel.Warning, "Topic could not be determined for message: {message}")]
        partial void LogTopicUnavailable(T message);



        private class TopicHashMaskSubscriptions {
            public Dictionary<ulong, HashSet<Subscription<T>>> SubscriptionsByHashMask { get; } = new Dictionary<ulong, HashSet<Subscription<T>>>();

            public void AddSubscription(Subscription<T> subscriber, SubscriptionTopic subscriptionTopic) {
                if (!SubscriptionsByHashMask.TryGetValue(subscriptionTopic.TopicHashMask, out var subscriptions)) {
                    subscriptions = new HashSet<Subscription<T>>();
                    SubscriptionsByHashMask.Add(subscriptionTopic.TopicHashMask, subscriptions);
                }
                subscriptions.Add(subscriber);
            }

            public void RemoveSubscription(Subscription<T> subscriber, SubscriptionTopic subscriptionTopic) {
                if (SubscriptionsByHashMask.TryGetValue(subscriptionTopic.TopicHashMask, out var subscriptions)) {
                    subscriptions.Remove(subscriber);
                    if (subscriptions.Count == 0) {
                        SubscriptionsByHashMask.Remove(subscriptionTopic.TopicHashMask);
                    }
                }
            }
        }

    }
}
