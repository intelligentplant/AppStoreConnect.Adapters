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
        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, CreateEventMessageSubscriptionRequest request) {
            var result = new Subscription(
                this, 
                context, 
                request
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
            private readonly CreateEventMessageSubscriptionRequest _request;

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
            /// <param name="request">
            ///   The subscription type.
            /// </param>
            public Subscription(
                EventMessagePushImpl feature, 
                IAdapterCallContext context,
                CreateEventMessageSubscriptionRequest request
            ) : base(context, feature.AdapterId, request?.SubscriptionType ?? EventMessageSubscriptionType.Active) {
                _feature = feature;
                _request = request ?? new CreateEventMessageSubscriptionRequest();
                _client = _feature.CreateClient<EventsService.EventsServiceClient>();
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(CancellationToken cancellationToken) {
                var request = new CreateEventPushChannelRequest() {
                    AdapterId = _feature.AdapterId,
                    SubscriptionType = _request.SubscriptionType == EventMessageSubscriptionType.Active
                        ? EventSubscriptionType.Active
                        : EventSubscriptionType.Passive
                };

                if (_request.Properties != null) {
                    foreach (var item in _request.Properties) {
                        request.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                using (var grpcChannel = _client.CreateEventPushChannel(
                   request,
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
