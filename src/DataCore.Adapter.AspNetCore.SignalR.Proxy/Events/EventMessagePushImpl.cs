using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {

    /// <summary>
    /// Implements <see cref="IEventMessagePush"/>.
    /// </summary>
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public EventMessagePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            return GetClient().Events.CreateEventMessageChannelAsync(
                AdapterId,
                request,
                cancellationToken
            );
        }


        /// <summary>
        /// <see cref="IEventMessageSubscription"/> implementation for the 
        /// <see cref="IEventMessagePush"/> feature.
        /// </summary>
        private class Subscription : EventMessageSubscriptionBase {

            /// <summary>
            /// The feature instance.
            /// </summary>
            private readonly EventMessagePushImpl _feature;

            /// <summary>
            /// The subscription request.
            /// </summary>
            private readonly CreateEventMessageSubscriptionRequest _request;

            /// <summary>
            /// The underlying hub connection.
            /// </summary>
            private readonly AdapterSignalRClient _client;


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="feature">
            ///   The feature instance.
            /// </param>
            /// <param name="context">
            ///   The adapter call context for the subscriber.
            /// </param>
            /// <param name="request">
            ///   Additional subscription request properties.
            /// </param>
            public Subscription( 
                EventMessagePushImpl feature,
                IAdapterCallContext context,
                CreateEventMessageSubscriptionRequest request
            ) : base(context, feature.AdapterId, request?.SubscriptionType ?? EventMessageSubscriptionType.Active) {
                _feature = feature;
                _request = request ?? new CreateEventMessageSubscriptionRequest();
                _client = feature.GetClient();
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(CancellationToken cancellationToken) {
                var eventsChannel = await _client.Events.CreateEventMessageChannelAsync(
                    _feature.AdapterId,
                    _request,
                    cancellationToken
                ).ConfigureAwait(false);

                while (await eventsChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!eventsChannel.TryRead(out var item) || item == null) {
                        continue;
                    }

                    await ValueReceived(item, cancellationToken).ConfigureAwait(false);
                }
            }

        }
    }
}
