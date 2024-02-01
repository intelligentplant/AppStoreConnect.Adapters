#if NETFRAMEWORK

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Grpc.Proxy.Events {

    partial class EventMessagePushWithTopicsImpl {

        /// <summary>
        /// All active streaming calls, indexed by subscription ID and then tag ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IDisposable>> _activeStreamingCalls = new ConcurrentDictionary<string, ConcurrentDictionary<string, IDisposable>>(StringComparer.Ordinal);


        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopics"/> instance that will handle all 
        /// individual tag subscriptions for a single call to <see cref="Subscribe"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="client">
        ///   The gRPC client to use.
        /// </param>
        /// <param name="request">
        ///   The initial <see cref="CreateEventMessageTopicSubscriptionRequest"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A new <see cref="EventMessagePushWithTopics"/> instance.
        /// </returns>
        private EventMessagePushWithTopics CreateInnerHandler(
            IAdapterCallContext context,
            EventsService.EventsServiceClient client,
            CreateEventMessageTopicSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            var options = new EventMessagePushWithTopicsOptions() {
                OnTopicSubscriptionsAdded = async (instance, topics, _) => await OnTagsAddedAsync(instance, context, client, topics, request, cancellationToken).ConfigureAwait(false),
                OnTopicSubscriptionsRemoved = async (instance, topics, _) => await OnTagsRemovedAsync(instance, topics).ConfigureAwait(false)
            };
            var result = new EventMessagePushWithTopics(options, BackgroundTaskService, Proxy.LoggerFactory.CreateLogger<EventMessagePushWithTopics>());

            return result;
        }


        /// <summary>
        /// Called when tags are added to the subscription.
        /// </summary>
        private Task OnTagsAddedAsync(
            EventMessagePushWithTopics instance,
            IAdapterCallContext context,
            EventsService.EventsServiceClient client,
            IEnumerable<string> topics,
            CreateEventMessageTopicSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            foreach (var topic in topics) {
                cancellationToken.ThrowIfCancellationRequested();

                var grpcRequest = new CreateEventTopicPushChannelMessage() {
                    AdapterId = Proxy.RemoteDescriptor.Id,
                    SubscriptionType = request.SubscriptionType == EventMessageSubscriptionType.Active
                        ? EventSubscriptionType.Active
                        : EventSubscriptionType.Passive
                };

                grpcRequest.Topics.Add(topic);

                if (request.Properties != null) {
                    foreach (var item in request.Properties) {
                        grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                var subscription = client.CreateFixedEventTopicPushChannel(grpcRequest, GetCallOptions(context, cancellationToken));

                var activeCallsForSubscriber = _activeStreamingCalls.GetOrAdd(instance.Id, _ => new ConcurrentDictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase));
                activeCallsForSubscriber[topic] = subscription;

                BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    while (await subscription.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (subscription.ResponseStream.Current == null) {
                            continue;
                        }
                        await instance.ValueReceived(subscription.ResponseStream.Current.ToAdapterEventMessage(), ct).ConfigureAwait(false);
                    }
                }, cancellationToken);
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Called when topics are removed from the subscription.
        /// </summary>
        private Task OnTagsRemovedAsync(
            EventMessagePushWithTopics instance,
            IEnumerable<string> topics
        ) {
            if (!_activeStreamingCalls.TryGetValue(instance.Id, out var activeCallsForSubscriber)) {
                return Task.CompletedTask;
            }

            foreach (var topic in topics) {
                if (activeCallsForSubscriber.TryRemove(topic, out var subscription)) {
                    subscription.Dispose();
                }
            }

            return Task.CompletedTask;
        }



        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.Events.EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<EventsService.EventsServiceClient>();

            using var handler = CreateInnerHandler(context, client, request, cancellationToken);

            try {
                await foreach (var item in handler.Subscribe(context, request, channel, cancellationToken).ConfigureAwait(false)) {
                    yield return item;
                }
            }
            finally {
                if (_activeStreamingCalls.TryRemove(handler.Id, out var activeCallsForSubscriber)) {
                    foreach (var item in activeCallsForSubscriber.Values) {
                        item.Dispose();
                    }
                }
            }
        }

    }

}

#endif
