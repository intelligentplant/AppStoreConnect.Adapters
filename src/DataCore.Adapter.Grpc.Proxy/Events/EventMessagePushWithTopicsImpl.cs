using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

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
        public Task<ChannelReader<Adapter.Events.EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageTopicSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            GrpcAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessage>(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();

                var grpcRequest = new CreateEventTopicPushChannelRequest() {
                    AdapterId = AdapterId,
                    SubscriptionType = request.SubscriptionType == EventMessageSubscriptionType.Active
                        ? EventSubscriptionType.Active
                        : EventSubscriptionType.Passive
                };

                grpcRequest.Topics.Add(request.Topics);

                if (request.Properties != null) {
                    foreach (var item in request.Properties) {
                        grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                using (var grpcChannel = client.CreateEventTopicPushChannel(
                   grpcRequest,
                   GetCallOptions(context, ct)
                )) {
                    // Read event messages.
                    while (await grpcChannel.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                        if (grpcChannel.ResponseStream.Current == null) {
                            continue;
                        }

                        await result.Writer.WriteAsync(grpcChannel.ResponseStream.Current.ToAdapterEventMessage(), ct).ConfigureAwait(false);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
