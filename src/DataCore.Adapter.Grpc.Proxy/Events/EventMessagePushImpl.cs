using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            IEventMessageSubscription result = new EventMessageSubscription(this, CreateClient<EventsService.EventsServiceClient>(), subscriptionType);
            try {
                await result.StartAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch {
                await result.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            return result;
        }


        private class EventMessageSubscription : EventMessageSubscriptionBase {

            private readonly EventMessagePushImpl _feature;

            private readonly EventsService.EventsServiceClient _client;

            private readonly bool _activeSubscription;


            public EventMessageSubscription(EventMessagePushImpl feature, EventsService.EventsServiceClient client, EventMessageSubscriptionType subscriptionType) {
                _feature = feature;
                _client = client;
                _activeSubscription = subscriptionType == EventMessageSubscriptionType.Active;
            }


            protected override ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
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

                return default;
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
