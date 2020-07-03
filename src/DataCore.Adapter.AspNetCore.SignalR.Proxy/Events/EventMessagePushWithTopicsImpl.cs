using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events {

    /// <summary>
    /// Implements <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    internal class EventMessagePushWithTopicsImpl : ProxyAdapterFeature, IEventMessagePushWithTopics {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopicsImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public EventMessagePushWithTopicsImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<IEventMessageSubscriptionWithTopics> Subscribe(IAdapterCallContext context, CreateEventMessageSubscriptionRequest request) {
            var result = new Subscription(
                this,
                context,
                request
            );

            await result.Start().ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// <see cref="IEventMessageSubscriptionWithTopics"/> implementation for the 
        /// <see cref="IEventMessagePushWithTopics"/> feature.
        /// </summary>
        private class Subscription : EventMessageSubscriptionWithTopicsBase {

            /// <summary>
            /// The feature instance.
            /// </summary>
            private readonly EventMessagePushWithTopicsImpl _feature;

            /// <summary>
            /// The subscription request.
            /// </summary>
            private readonly CreateEventMessageSubscriptionRequest _request;

            /// <summary>
            /// The remote subscription ID.
            /// </summary>
            private string _subscriptionId;

            /// <summary>
            /// Holds the lifetime cancellation token for each subscribed topic.
            /// </summary>
            private readonly ConcurrentDictionary<string, CancellationTokenSource> _topicSubscriptionLifetimes = new ConcurrentDictionary<string, CancellationTokenSource>();


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="feature">
            ///   The feature instance.
            /// </param>
            /// <param name="context">
            ///   The adapter call context for the subscriber.
            /// </param>
            /// <param name="request">
            ///   Additional subscription request properties.
            /// </param>
            public Subscription(
                EventMessagePushWithTopicsImpl feature,
                IAdapterCallContext context,
                CreateEventMessageSubscriptionRequest request
            ) : base(context, feature.AdapterId, request?.SubscriptionType ?? EventMessageSubscriptionType.Active) {
                _feature = feature;
                _request = request ?? new CreateEventMessageSubscriptionRequest();
            }


            /// <inheritdoc/>
            protected override async Task Init(CancellationToken cancellationToken) {
                await base.Init(cancellationToken).ConfigureAwait(false);

                // Create the subscription.
                _subscriptionId = await _feature.GetClient().Events.CreateEventMessageTopicSubscriptionAsync(
                    _feature.AdapterId,
                    _request,
                    cancellationToken
                ).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            protected override Task OnTopicAdded(string topic) {
                if (CancellationToken.IsCancellationRequested) {
                    return Task.CompletedTask;
                }

                var added = false;
                var ctSource = _topicSubscriptionLifetimes.GetOrAdd(topic, k => {
                    added = true;
                    return new CancellationTokenSource();
                });

                if (added) {
                    var tcs = new TaskCompletionSource<bool>();
                    _feature.TaskScheduler.QueueBackgroundWorkItem(ct => RunTopicSubscription(topic, tcs, ct), ctSource.Token, CancellationToken);
                    return tcs.Task;
                }

                return Task.CompletedTask;
            }


            /// <inheritdoc/>
            protected override Task OnTopicRemoved(string topic) {
                if (CancellationToken.IsCancellationRequested) {
                    return Task.CompletedTask;
                }

                if (_topicSubscriptionLifetimes.TryRemove(topic, out var ctSource)) {
                    ctSource.Cancel();
                    ctSource.Dispose();
                }

                return Task.CompletedTask;
            }


            /// <inheritdoc/>
            protected override void OnCancelled() {
                base.OnCancelled();

                foreach (var item in _topicSubscriptionLifetimes.Values.ToArray()) {
                    item.Cancel();
                    item.Dispose();
                }

                _topicSubscriptionLifetimes.Clear();
                if (!string.IsNullOrWhiteSpace(_subscriptionId)) {
                    // Notify server of cancellation.
                    _feature.TaskScheduler.QueueBackgroundWorkItem(ct => _feature.GetClient().Events.DeleteEventMessageTopicSubscriptionAsync(_subscriptionId, ct));
                }
            }


            /// <summary>
            /// Creates and processes a subscription to the specified topic.
            /// </summary>
            /// <param name="topic">
            ///   The topic.
            /// </param>
            /// <param name="tcs">
            ///   A <see cref="TaskCompletionSource{TResult}"/> that will be completed once the 
            ///   tag subscription has been created.
            /// </param>
            /// <param name="cancellationToken">
            ///   The cancellation token for the operation.
            /// </param>
            /// <returns>
            ///   A long-running task that will run the subscription until the cancellation token 
            ///   fires.
            /// </returns>
            private async Task RunTopicSubscription(string topic, TaskCompletionSource<bool> tcs, CancellationToken cancellationToken) {
                ChannelReader<EventMessage> hubChannel;

                try {
                    hubChannel = await _feature.GetClient().Events.CreateEventMessageTopicChannelAsync(
                        _subscriptionId,
                        topic,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    tcs.TrySetCanceled(cancellationToken);
                    throw;
                }
                catch (Exception e) {
                    tcs.TrySetException(e);
                    throw;
                }
                finally {
                    tcs.TrySetResult(true);
                }

                await hubChannel.ForEachAsync(async val => {
                    if (val == null) {
                        return;
                    }
                    await ValueReceived(val, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }

        }

    }
}
