using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events {

    /// <summary>
    /// <see cref="IEventMessagePushWithTopics"/> implementation.
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
        public async Task<ChannelReader<Adapter.Events.EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageTopicSubscriptionRequest request,
            ChannelReader<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            GrpcAdapterProxy.ValidateObject(request);

            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = CreateClient<EventsService.EventsServiceClient>();
            var grpcStream = client.CreateEventTopicPushChannel(GetCallOptions(context, cancellationToken));

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

            // Create the subscription.
            await grpcStream.RequestStream.WriteAsync(new CreateEventTopicPushChannelRequest() {
                Create = createSubscriptionMessage
            }).ConfigureAwait(false);

            // Stream subscription changes.
            channel.RunBackgroundOperation(async (ch, ct) => {
                while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    while (ch.TryRead(out var update)) {
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
                }
            }, BackgroundTaskService, cancellationToken);

            // Stream the results.
            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessage>(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                // Read tag values.
                while (await grpcStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcStream.ResponseStream.Current == null) {
                        continue;
                    }

                    await result.Writer.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterEventMessage(), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
