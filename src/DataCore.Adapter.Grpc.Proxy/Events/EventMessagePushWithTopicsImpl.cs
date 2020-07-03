using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

using IntelligentPlant.BackgroundTasks;

using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy.Events {

    /// <summary>
    /// <see cref="IEventMessageSubscriptionWithTopics"/> implementation.
    /// </summary>
    internal class EventMessagePushWithTopicsImpl : ProxyAdapterFeature, IEventMessagePushWithTopics {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopicsImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public EventMessagePushWithTopicsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<IEventMessageSubscriptionWithTopics> Subscribe(IAdapterCallContext context, CreateEventMessageSubscriptionRequest request) {
            var result = new Subscription(
                this,
                context,
                request
            );
            await result.Start().ConfigureAwait(false);
            return result;
        }


        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            try {
                // Ensure that we delete all subscriptions for this connection.
                var request = new DeleteEventTopicPushSubscriptionRequest() {
                    SessionId = RemoteSessionId,
                    SubscriptionId = string.Empty
                };
                var response = CreateClient<EventsService.EventsServiceClient>().DeleteEventTopicPushSubscriptionAsync(request, GetCallOptions(null, default));
                await response.ResponseAsync.ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
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
            /// The client for the gRPC service.
            /// </summary>
            private readonly EventsService.EventsServiceClient _client;

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
                _client = _feature.CreateClient<EventsService.EventsServiceClient>();
            }


            /// <inheritdoc/>
            protected override async Task Init(CancellationToken cancellationToken) {
                await base.Init(cancellationToken).ConfigureAwait(false);

                // Create the subscription.
                var request = new CreateEventTopicPushSubscriptionRequest() {
                    SessionId = _feature.RemoteSessionId,
                    AdapterId = _feature.AdapterId
                };
                if (_request.Properties != null) {
                    foreach (var item in _request.Properties) {
                        request.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                var response = _client.CreateEventTopicPushSubscriptionAsync(
                    request,
                    _feature.GetCallOptions(Context, cancellationToken)
                );

                var result = await response.ResponseAsync.ConfigureAwait(false);
                _subscriptionId = result.SubscriptionId;
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
                    _feature.TaskScheduler.QueueBackgroundWorkItem(async ct => {
                        var response = _client.DeleteEventTopicPushSubscriptionAsync(new DeleteEventTopicPushSubscriptionRequest() {
                            SubscriptionId = _subscriptionId
                        }, _feature.GetCallOptions(Context, ct));

                        await response.ResponseAsync.ConfigureAwait(false);
                    });
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
                if (string.IsNullOrWhiteSpace(_subscriptionId)) {
                    tcs.TrySetResult(false);
                    return;
                }

                GrpcCore.AsyncServerStreamingCall<EventMessage> grpcChannel;

                try {
                    grpcChannel = _client.CreateEventTopicPushChannel(new CreateEventTopicPushChannelRequest() {
                        SessionId = _feature.RemoteSessionId,
                        SubscriptionId = _subscriptionId,
                        Topic = topic
                    }, _feature.GetCallOptions(Context, cancellationToken));
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

                while (await grpcChannel.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcChannel.ResponseStream.Current == null) {
                        continue;
                    }

                    await ValueReceived(
                        grpcChannel.ResponseStream.Current.ToAdapterEventMessage(),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }

        }

    }
}
