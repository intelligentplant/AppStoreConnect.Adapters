using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, Adapter.Events.Models.EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            var result = new EventMessageSubscription(this, CreateClient<EventsService.EventsServiceClient>(), subscriptionType);
            result.Start(context);
            return Task.FromResult<IEventMessageSubscription>(result);
        }


        private class EventMessageSubscription : IEventMessageSubscription {

            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            private readonly EventMessagePushImpl _feature;

            private readonly EventsService.EventsServiceClient _client;

            private readonly System.Threading.Channels.Channel<Adapter.Events.Models.EventMessage> _channel = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.Models.EventMessage>();

            private readonly bool _activeSubscription;

            public System.Threading.Channels.ChannelReader<Adapter.Events.Models.EventMessage> Reader { get { return _channel; } }


            public EventMessageSubscription(EventMessagePushImpl feature, EventsService.EventsServiceClient client, Adapter.Events.Models.EventMessageSubscriptionType subscriptionType) {
                _feature = feature;
                _client = client;
                _activeSubscription = subscriptionType == Adapter.Events.Models.EventMessageSubscriptionType.Active;
            }


            public void Start(IAdapterCallContext context) {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var grpcResponse = _client.CreateEventPushChannel(new CreateEventPushChannelRequest() {
                        AdapterId = _feature.AdapterId,
                        SubscriptionType = _activeSubscription 
                            ? EventSubscriptionType.Active 
                            : EventSubscriptionType.Passive
                    }, _feature.GetCallOptions(context, ct));

                    try {
                        while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                            if (grpcResponse.ResponseStream.Current == null) {
                                continue;
                            }
                            await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterEventMessage(), ct).ConfigureAwait(false);
                        }
                    }
                    finally {
                        grpcResponse.Dispose();
                    }
                }, false, _shutdownTokenSource.Token);
            }


            public void Dispose() {
                _shutdownTokenSource.Cancel();
                _shutdownTokenSource.Dispose();
                _channel.Writer.TryComplete();
            }
        }
    }
}
