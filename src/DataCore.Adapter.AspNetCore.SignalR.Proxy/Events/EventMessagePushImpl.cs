using System;
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
        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            IEventMessageSubscription result = new EventMessageSubscription(
                this,
                subscriptionType
            );

            try {
                await result.StartAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch {
                await result.DisposeAsync().ConfigureAwait(false);
                throw;
            }
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
            /// <param name="subscriptionType">
            ///   Flags if the subscription is active or passive.
            /// </param>
            public EventMessageSubscription(EventMessagePushImpl feature, EventMessageSubscriptionType subscriptionType) {
                _client = feature.GetClient();
                _subscriptionType = subscriptionType;
            }

            /// <inheritdoc />
            protected override ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                Writer.RunBackgroundOperation(async (ch, ct) => {
                    var hubChannel = await _client.Events.CreateEventMessageChannelAsync(_feature.AdapterId, _subscriptionType, ct).ConfigureAwait(false);
                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, _feature.TaskScheduler, SubscriptionCancelled);

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
