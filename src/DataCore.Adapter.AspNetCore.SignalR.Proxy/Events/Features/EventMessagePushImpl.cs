using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;

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
        public Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            var result = new EventMessageSubscription(
                AdapterId,
                GetClient(),
                subscriptionType
            );
            result.Start();
            return Task.FromResult<IEventMessageSubscription>(result);
        }

        /// <summary>
        /// <see cref="IEventMessageSubscription"/> implementation for the 
        /// <see cref="IEventMessagePush"/> feature.
        /// </summary>
        private class EventMessageSubscription : IEventMessageSubscription {

            /// <summary>
            /// Fires when the subscription is disposed.
            /// </summary>
            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            /// <summary>
            /// The adapter ID for the subscription.
            /// </summary>
            private readonly string _adapterId;

            /// <summary>
            /// The underlying hub connection.
            /// </summary>
            private readonly AdapterSignalRClient _client;

            /// <summary>
            /// The channel for the subscription.
            /// </summary>
            private readonly Channel<EventMessage> _channel = ChannelExtensions.CreateEventMessageChannel<EventMessage>(-1);

            /// <summary>
            /// Flags if the subscription is active or passive.
            /// </summary>
            private readonly EventMessageSubscriptionType _subscriptionType;

            /// <inheritdoc />
            public ChannelReader<EventMessage> Reader { get { return _channel; } }


            /// <summary>
            /// Creates a new <see cref="EventMessageSubscription"/> object.
            /// </summary>
            /// <param name="adapterId">
            ///   The adapter ID.
            /// </param>
            /// <param name="client">
            ///   The adapter SignalR client.
            /// </param>
            /// <param name="subscriptionType">
            ///   Flags if the subscription is active or passive.
            /// </param>
            public EventMessageSubscription(string adapterId, AdapterSignalRClient client, EventMessageSubscriptionType subscriptionType) {
                _adapterId = adapterId;
                _client = client;
                _subscriptionType = subscriptionType;
            }

            /// <summary>
            /// Starts the subscription.
            /// </summary>
            public void Start() {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var hubChannel = await _client.Events.CreateEventMessageChannelAsync(_adapterId, _subscriptionType, ct).ConfigureAwait(false);
                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, _shutdownTokenSource.Token);
            }

            /// <inheritdoc />
            public void Dispose() {
                _shutdownTokenSource.Cancel();
                _shutdownTokenSource.Dispose();
                _channel.Writer.TryComplete();
            }
        }
    }
}
