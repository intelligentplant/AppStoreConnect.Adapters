using System.Threading;
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
        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) {
            var result = new Subscription(
                this, 
                context, 
                subscriptionType
            );
            await result.Start().ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// <see cref="EventMessageSubscriptionBase"/> implementation that receives data via a 
        /// gRPC channel.
        /// </summary>
        private class Subscription : EventMessageSubscriptionBase {

            /// <summary>
            /// The creating feature.
            /// </summary>
            private readonly EventMessagePushImpl _feature;

            /// <summary>
            /// Indicates if the subscription is active or passive.
            /// </summary>
            private readonly bool _activeSubscription;

            /// <summary>
            /// The client for the gRPC service.
            /// </summary>
            private readonly EventsService.EventsServiceClient _client;


            /// <summary>
            /// Creates a new <see cref="Subscription"/>.
            /// </summary>
            /// <param name="context">
            ///   The adapter call context for the subscription owner.
            /// </param>
            /// <param name="feature">
            ///   The push feature.
            /// </param>
            /// <param name="subscriptionType">
            ///   The subscription type.
            /// </param>
            public Subscription(
                EventMessagePushImpl feature, 
                IAdapterCallContext context,
                EventMessageSubscriptionType subscriptionType
            ) : base(context, feature.AdapterId, subscriptionType) {
                _feature = feature;
                _activeSubscription = SubscriptionType == EventMessageSubscriptionType.Active;
                _client = _feature.CreateClient<EventsService.EventsServiceClient>();
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(CancellationToken cancellationToken) {
                using (var grpcChannel = _client.CreateEventPushChannel(
                   new CreateEventPushChannelRequest() {
                       AdapterId = _feature.AdapterId,
                       SubscriptionType = _activeSubscription
                           ? EventSubscriptionType.Active
                           : EventSubscriptionType.Passive
                   },
                   _feature.GetCallOptions(Context, cancellationToken)
                )) {
                    // Read event messages.
                    while (await grpcChannel.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                        if (grpcChannel.ResponseStream.Current == null) {
                            continue;
                        }

                        await ValueReceived(
                            grpcChannel.ResponseStream.Current.ToAdapterEventMessage(),
                            cancellationToken
                        ).ConfigureAwait(false);
                    }
                }
            }

        }
    }
}
