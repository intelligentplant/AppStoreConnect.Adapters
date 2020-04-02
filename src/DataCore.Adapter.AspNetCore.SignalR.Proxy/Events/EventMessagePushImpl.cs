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
        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) {
            var result = new EventMessageSubscription(
                this,
                context,
                subscriptionType
            );

            await result.Start().ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// <see cref="IEventMessageSubscription"/> implementation for the 
        /// <see cref="IEventMessagePush"/> feature.
        /// </summary>
        private class EventMessageSubscription : Adapter.Events.EventMessageSubscriptionBase {

            /// <summary>
            /// The feature instance.
            /// </summary>
            private readonly EventMessagePushImpl _feature;

            /// <summary>
            /// The underlying hub connection.
            /// </summary>
            private readonly AdapterSignalRClient _client;

            /// <summary>
            /// The channel reader to read messages from.
            /// </summary>
            private ChannelReader<EventMessage> _eventsChannel;


            /// <summary>
            /// Creates a new <see cref="EventMessageSubscription"/> object.
            /// </summary>
            /// <param name="feature">
            ///   The feature instance.
            /// </param>
            /// <param name="context">
            ///   The adapter call context for the subscriber.
            /// </param>
            /// <param name="subscriptionType">
            ///   Flags if the subscription is active or passive.
            /// </param>
            public EventMessageSubscription( 
                EventMessagePushImpl feature,
                IAdapterCallContext context,
                EventMessageSubscriptionType subscriptionType
            ) : base(context, feature.AdapterId, subscriptionType) {
                _feature = feature;
                _client = feature.GetClient();
            }


            /// <inheritdoc/>
            protected override async Task Init(CancellationToken cancellationToken) {
                _eventsChannel = await _client.Events.CreateEventMessageChannelAsync(
                    _feature.AdapterId,
                    SubscriptionType,
                    CancellationToken
                ).ConfigureAwait(false);

                // Wait for and discard the initial "subscription created" placeholder message.
                await _eventsChannel.ReadAsync(cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(CancellationToken cancellationToken) {
                if (_eventsChannel == null) {
                    return;
                }

                while (await _eventsChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!_eventsChannel.TryRead(out var item) || item == null) {
                        continue;
                    }

                    await ValueReceived(item, cancellationToken).ConfigureAwait(false);
                }
            }

        }
    }
}
