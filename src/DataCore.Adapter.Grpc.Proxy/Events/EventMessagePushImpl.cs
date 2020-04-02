using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) {
            var result = new EventMessageSubscription(
                this, 
                context, 
                subscriptionType
            );
            await result.Start().ConfigureAwait(false);
            return result;
        }


        private class EventMessageSubscription : Adapter.Events.EventMessageSubscriptionBase {

            private readonly EventMessagePushImpl _feature;

            private readonly bool _activeSubscription;

            private GrpcCore.AsyncServerStreamingCall<EventMessage> _streamingCall;


            public EventMessageSubscription(
                EventMessagePushImpl feature, 
                IAdapterCallContext context,
                EventMessageSubscriptionType subscriptionType
            ) : base(context, feature.AdapterId, subscriptionType) {
                _feature = feature;
                _activeSubscription = SubscriptionType == EventMessageSubscriptionType.Active;
            }


            /// <inheritdoc/>
            protected override async Task Init(CancellationToken cancellationToken) {
                var client = _feature.CreateClient<EventsService.EventsServiceClient>();
                _streamingCall = client.CreateEventPushChannel(
                    new CreateEventPushChannelRequest() {
                        AdapterId = _feature.AdapterId,
                        SubscriptionType = _activeSubscription
                            ? EventSubscriptionType.Active
                            : EventSubscriptionType.Passive
                    },
                    _feature.GetCallOptions(Context, cancellationToken)
                );
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(CancellationToken cancellationToken) {
                if (_streamingCall == null) {
                    return;
                }

                // Wait for and discard the initial "subscription created" placeholder message.
                await _streamingCall.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);

                // Read value changes.
                while (await _streamingCall.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (_streamingCall.ResponseStream.Current == null) {
                        continue;
                    }

                    await ValueReceived(
                        _streamingCall.ResponseStream.Current.ToAdapterEventMessage(),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }


            /// <inheritdoc/>
            protected override void OnCancelled() {
                base.OnCancelled();
                _streamingCall?.Dispose();
            }

        }
    }
}
