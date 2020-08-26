using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    /// <summary>
    /// <see cref="IEventMessagePush"/> implementation.
    /// </summary>
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.Events.EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessage>(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();

                var grpcRequest = new CreateEventPushChannelRequest() {
                    AdapterId = AdapterId,
                    SubscriptionType = request.SubscriptionType == EventMessageSubscriptionType.Active
                        ? EventSubscriptionType.Active
                        : EventSubscriptionType.Passive
                };

                if (request.Properties != null) {
                    foreach (var item in request.Properties) {
                        grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                using (var grpcChannel = client.CreateEventPushChannel(
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
