using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, Adapter.Events.Models.EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            var result = new EventMessageSubscription(this, CreateClient<EventsService.EventsServiceClient>(), subscriptionType);
            result.Start(context);
            return Task.FromResult<IEventMessageSubscription>(result);
        }


        private class EventMessageSubscription : EventMessageSubscriptionBase {

            private readonly EventMessagePushImpl _feature;

            private readonly EventsService.EventsServiceClient _client;

            private readonly bool _activeSubscription;


            public EventMessageSubscription(EventMessagePushImpl feature, EventsService.EventsServiceClient client, Adapter.Events.Models.EventMessageSubscriptionType subscriptionType) {
                _feature = feature;
                _client = client;
                _activeSubscription = subscriptionType == Adapter.Events.Models.EventMessageSubscriptionType.Active;
            }


            public void Start(IAdapterCallContext context) {
                Writer.RunBackgroundOperation(async (ch, ct) => {
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
                }, false, SubscriptionCancelled);
            }


            /// <inheritdoc />
            protected override void Dispose(bool disposing) {
                // Do nothing.
            }


            /// <inheritdoc />
            protected override ValueTask DisposeAsync(bool disposing) {
                Dispose(disposing);
                return default;
            }

        }
    }
}
