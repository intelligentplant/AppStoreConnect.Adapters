using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Implements <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    public class EventMessagePushWithTopics : SubscriptionManager<EventMessagePushWithTopicsOptions, string, EventMessage, EventSubscriptionChannel>, IEventMessagePushWithTopics {

        /// <summary>
        /// Channel that is used to publish changes to subscribed topics.
        /// </summary>
        private readonly Channel<(List<string> Topics, bool Added, TaskCompletionSource<bool> Processed)> _topicSubscriptionChangesChannel = Channel.CreateUnbounded<(List<string>, bool, TaskCompletionSource<bool>)>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// Maps from topic name to the subscriber count for that topic.
        /// </summary>
        private readonly Dictionary<string, int> _subscriberCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }


        /// <summary>
        /// Creates a new <see cref="EventMessagePush"/> object.
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
        public EventMessagePushWithTopics(EventMessagePushWithTopicsOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger)
            : base(options, backgroundTaskService, logger) { 
            BackgroundTaskService.QueueBackgroundWorkItem(ProcessTopicSubscriptionChangesChannel, DisposedToken);
        }


        /// <inheritdoc/>
        public async Task<ChannelReader<EventMessage>> Subscribe(IAdapterCallContext context, CreateEventMessageTopicSubscriptionRequest request, ChannelReader<EventMessageSubscriptionUpdate> channel, CancellationToken cancellationToken) {
            if (IsDisposed) {
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

            var subscription = CreateSubscription(context, request, cancellationToken);
            if (request.Topics != null && request.Topics.Any()) {
                await OnTopicsAddedToSubscriptionInternal(subscription, request.Topics, cancellationToken).ConfigureAwait(false);
            }

            BackgroundTaskService.QueueBackgroundWorkItem(ct => RunSubscriptionChangesListener(subscription, channel, ct), subscription.CancellationToken);

            return subscription.Reader;
        }


        /// <inheritdoc/>
        protected override EventSubscriptionChannel CreateSubscriptionChannel(
            IAdapterCallContext context, 
            int id, 
            int channelCapacity,
            CancellationToken[] cancellationTokens, 
            Action cleanup, 
            object? state
        ) {
            var request = (CreateEventMessageTopicSubscriptionRequest) state!;
            return new EventSubscriptionChannel(
                id,
                context,
                BackgroundTaskService,
                request?.SubscriptionType ?? EventMessageSubscriptionType.Active,
                TimeSpan.Zero,
                cancellationTokens,
                cleanup,
                channelCapacity
            );
        }


        /// <inheritdoc/>
        protected override void OnSubscriptionAdded(EventSubscriptionChannel subscription) {
            base.OnSubscriptionAdded(subscription);
            HasActiveSubscriptions = HasSubscriptions && GetSubscriptions().Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
        }


        /// <inheritdoc/>
        protected override void OnSubscriptionCancelled(EventSubscriptionChannel subscription) {
            base.OnSubscriptionCancelled(subscription);
            HasActiveSubscriptions = HasSubscriptions && GetSubscriptions().Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
            OnTopicsRemovedFromSubscriptionInternal(subscription!, subscription.Topics);
        }


        /// <inheritdoc/>
        protected override bool IsTopicMatch(EventMessage value, IEnumerable<string> topics) {
            if (value?.Topic == null) {
                return false;
            }

            if (Options.IsTopicMatch != null) {
                return topics.Any(x => Options.IsTopicMatch(x, value.Topic));
            }

            if (topics.Any(x => string.Equals(value.Topic, x, StringComparison.Ordinal))) {
                return true;
            }

            return false;
        }


        /// <inheritdoc/>
        protected override IDictionary<string, string> GetHealthCheckProperties(IAdapterCallContext context) {
            var result = base.GetHealthCheckProperties(context);

            var subscriptions = GetSubscriptions();

            result[Resources.HealthChecks_Data_ActiveSubscriberCount] = subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Active).ToString(context?.CultureInfo);
            result[Resources.HealthChecks_Data_PassiveSubscriberCount] = subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Passive).ToString(context?.CultureInfo);

            return result;
        }


        /// <summary>
        /// Called when the number of subscribers for a topic changes from zero to one.
        /// </summary>
        /// <param name="topics">
        ///   The topics that were added.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the change.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="topics"/> is <see langword="null"/>.
        /// </exception>
        protected virtual Task OnTopicsAdded(IEnumerable<string> topics, CancellationToken cancellationToken) {
            if (topics == null) {
                throw new ArgumentNullException(nameof(topics));
            }

            return Options.OnTopicSubscriptionsAdded == null
                ? Task.CompletedTask
                : Options.OnTopicSubscriptionsAdded.Invoke(topics, cancellationToken);
        }


        /// <summary>
        /// Called when the number of subscribers for a topic changes from one to zero.
        /// </summary>
        /// <param name="topics">
        ///   The topics that were removed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the change.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="topics"/> is <see langword="null"/>.
        /// </exception>
        protected virtual Task OnTopicsRemoved(IEnumerable<string> topics, CancellationToken cancellationToken) {
            if (topics == null) {
                throw new ArgumentNullException(nameof(topics));
            }

            return Options.OnTopicSubscriptionsRemoved == null
                ? Task.CompletedTask
                : Options.OnTopicSubscriptionsRemoved.Invoke(topics, cancellationToken);
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
        private async Task RunSubscriptionChangesListener(EventSubscriptionChannel subscription, ChannelReader<EventMessageSubscriptionUpdate> channel, CancellationToken cancellationToken) {
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item?.Topics == null) {
                        continue;
                    }

                    var topics = item.Topics.Where(x => x != null).ToArray();
                    if (topics.Length == 0) {
                        continue;
                    }

                    if (item.Action == SubscriptionUpdateAction.Subscribe) {
                        await OnTopicsAddedToSubscriptionInternal(subscription, topics, cancellationToken).ConfigureAwait(false);
                    }
                    else {
                        OnTopicsRemovedFromSubscriptionInternal(subscription, topics);
                    }
                }
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
        private async Task OnTopicsAddedToSubscriptionInternal(EventSubscriptionChannel subscription, IEnumerable<string> topics, CancellationToken cancellationToken) {
            TaskCompletionSource<bool> processed = null!;

            subscription.AddTopics(topics);

            lock (_subscriberCount) {
                var newSubscriptions = new List<string>();

                foreach (var topic in topics) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }
                    if (topic == null) {
                        continue;
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
            await processed.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
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
        private void OnTopicsRemovedFromSubscriptionInternal(EventSubscriptionChannel subscription, IEnumerable<string> topics) {
            lock (_subscriberCount) {
                var removedSubscriptions = new List<string>();

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
        private async Task ProcessTopicSubscriptionChangesChannel(CancellationToken cancellationToken) {
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
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    try {
                        if (change.Added) {
                            await OnTopicsAdded(change.Topics, cancellationToken).ConfigureAwait(false);
                        }
                        else {
                            await OnTopicsRemoved(change.Topics, cancellationToken).ConfigureAwait(false);
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
                            Resources.Log_ErrorWhileProcessingSubscriptionTopicChange,
                            change.Topics.Count,
                            change.Added
                                ? SubscriptionUpdateAction.Subscribe
                                : SubscriptionUpdateAction.Unsubscribe
                        );
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing) {
                _topicSubscriptionChangesChannel.Writer.TryComplete();
                lock (_subscriberCount) {
                    _subscriberCount.Clear();
                }
            }
        }

    }


    /// <summary>
    /// Options for <see cref="EventMessagePushWithTopics"/>.
    /// </summary>
    public class EventMessagePushWithTopicsOptions : SubscriptionManagerOptions {

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a topic changes from zero 
        /// to one.
        /// </summary>
        public Func<IEnumerable<string>, CancellationToken, Task>? OnTopicSubscriptionsAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a topic changes from one 
        /// to zero.
        /// </summary>
        public Func<IEnumerable<string>, CancellationToken, Task>? OnTopicSubscriptionsRemoved { get; set; }

        /// <summary>
        /// A delegate that is invoked to determine if a topic for a subscription matches the 
        /// topic for a received event message.
        /// </summary>
        /// <remarks>
        ///   The first parameter passed to the delegate is the subscription topic, and the second 
        ///   parameter is the topic for the received event message.
        /// </remarks>
        public Func<string, string?, bool>? IsTopicMatch { get; set; }

    }

}
