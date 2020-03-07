using System.Threading;
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
        public IEventMessageSubscription Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) {
            var result = new EventMessageSubscription(
                this,
                context,
                subscriptionType
            );

            result.Start();
            return result;
        }


        /// <summary>
        /// <see cref="IEventMessageSubscription"/> implementation for the 
        /// <see cref="IEventMessagePush"/> feature.
        /// </summary>
        private class EventMessageSubscription : Adapter.Events.EventMessageSubscription {

            /// <summary>
            /// The feature instance.
            /// </summary>
            private readonly EventMessagePushImpl _feature;

            /// <summary>
            /// The underlying hub connection.
            /// </summary>
            private readonly AdapterSignalRClient _client;

            /// <summary>
            /// Flags if the subscription is active or passive.
            /// </summary>
            private readonly EventMessageSubscriptionType _subscriptionType;


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
            ) : base(context) {
                _feature = feature;
                _client = feature.GetClient();
                _subscriptionType = subscriptionType;
            }


            /// <inheritdoc/>
            protected override async Task Run(CancellationToken cancellationToken) {
                var hubChannel = await _client.Events.CreateEventMessageChannelAsync(
                    _feature.AdapterId,
                    _subscriptionType,
                    CancellationToken
                ).ConfigureAwait(false);

                // Wait for initial "subscription ready" message.
                await hubChannel.ReadAsync().ConfigureAwait(false);

                while (await hubChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!hubChannel.TryRead(out var item) || item == null) {
                        continue;
                    }

                    await ValueReceived(item, cancellationToken).ConfigureAwait(false);
                }
            }

        }
    }
}
