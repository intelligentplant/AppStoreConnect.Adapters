#if NETFRAMEWORK == false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Proxy.Events {

    partial class EventMessagePushWithTopicsImpl {

        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.Events.EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = CreateClient<EventsService.EventsServiceClient>();

            var createSubscriptionMessage = new CreateEventTopicPushChannelMessage() {
                AdapterId = AdapterId,
                SubscriptionType = request.SubscriptionType == EventMessageSubscriptionType.Active
                    ? EventSubscriptionType.Active
                    : EventSubscriptionType.Passive
            };
            createSubscriptionMessage.Topics.Add(request.Topics?.Where(x => x != null) ?? Array.Empty<string>());
            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    createSubscriptionMessage.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcStream = client.CreateEventTopicPushChannel(GetCallOptions(context, ctSource.Token))) {

                // Create the subscription.
                await grpcStream.RequestStream.WriteAsync(new CreateEventTopicPushChannelRequest() {
                    Create = createSubscriptionMessage
                }).ConfigureAwait(false);

                // Stream subscription changes.
                Proxy.BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    await foreach (var update in channel.WithCancellation(ct).ConfigureAwait(false)) {
                        if (update == null) {
                            continue;
                        }

                        var msg = new UpdateEventTopicPushChannelMessage() {
                            Action = update.Action == Common.SubscriptionUpdateAction.Subscribe
                                ? SubscriptionUpdateAction.Subscribe
                                : SubscriptionUpdateAction.Unsubscribe
                        };
                        msg.Topics.Add(update.Topics.Where(x => x != null));
                        if (msg.Topics.Count == 0) {
                            continue;
                        }

                        await grpcStream.RequestStream.WriteAsync(new CreateEventTopicPushChannelRequest() {
                            Update = msg
                        }).ConfigureAwait(false);
                    }
                }, ctSource.Token);

                // Stream the results.
                while (await grpcStream.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcStream.ResponseStream.Current == null) {
                        continue;
                    }

                    yield return grpcStream.ResponseStream.Current.ToAdapterEventMessage();
                }
            }
        }

    }

}

#endif
