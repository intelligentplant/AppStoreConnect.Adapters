using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public IEventMessageSubscription Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) {
            IEventMessageSubscription result = new EventMessageSubscription(
                this, 
                context, 
                subscriptionType
            );
            result.Start();
            return result;
        }


        private class EventMessageSubscription : Adapter.Events.EventMessageSubscriptionBase {

            private readonly EventMessagePushImpl _feature;

            private readonly bool _activeSubscription;


            public EventMessageSubscription(
                EventMessagePushImpl feature, 
                IAdapterCallContext context,
                EventMessageSubscriptionType subscriptionType
            ) : base(context) {
                _feature = feature;
                _activeSubscription = subscriptionType == EventMessageSubscriptionType.Active;
            }


            /// <inheritdoc/>
            protected override async Task Run(CancellationToken cancellationToken) {
                var client = _feature.CreateClient<EventsService.EventsServiceClient>();
                var duplexCall = client.CreateEventPushChannel(
                    new CreateEventPushChannelRequest() { 
                        AdapterId = _feature.AdapterId,
                        SubscriptionType = _activeSubscription
                            ? EventSubscriptionType.Active
                            : EventSubscriptionType.Passive
                    },
                    _feature.GetCallOptions(Context, cancellationToken)
                );

                // Read value changes.
                while (await duplexCall.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (duplexCall.ResponseStream.Current == null) {
                        continue;
                    }

                    await ValueReceived(
                        duplexCall.ResponseStream.Current.ToAdapterEventMessage(),
                        false,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }

        }
    }
}
