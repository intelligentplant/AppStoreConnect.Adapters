using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Events;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

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
                _feature = feature;
                _client = feature.GetClient();
                _subscriptionType = subscriptionType;
                _client.Reconnected += OnClientReconnected;
            }


            /// <summary>
            /// Handles SignalR client reconnections.
            /// </summary>
            /// <param name="connectionId">
            ///   The updated connection ID.
            /// </param>
            /// <returns>
            ///   A task that will re-create the subscription to the remote adapter.
            /// </returns>
            private async Task OnClientReconnected(string connectionId) {
                await CreateSignalRChannel().ConfigureAwait(false);
            }


            /// <summary>
            /// Creates a SignalR subscription events from the remote adapter and then starts a 
            /// background task to forward received messages to this subscription's channel.
            /// </summary>
            /// <returns>
            ///   A task that will complete as soon as the subscription has been established. 
            ///   Forwarding of received events will continue in a background task.
            /// </returns>
            private async Task CreateSignalRChannel() {
                var hubChannel = await _client.Events.CreateEventMessageChannelAsync(
                    _feature.AdapterId,
                    _subscriptionType,
                    SubscriptionCancelled
                ).ConfigureAwait(false);

                _feature.TaskScheduler.QueueBackgroundWorkItem(async ct => {
                    try {
                        await hubChannel.Forward(Writer, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException e) {
                        // Subscription was cancelled.
                        Writer.TryComplete(e);
                    }
                    catch (Exception e) {
                        // Another error (e.g. SignalR disconnection) occurred. In this situation, 
                        // we won't complete the Writer in case we manage to reconnect.
                        _feature.Logger.LogError(e, Resources.Log_EventsSubscriptionError);
                    }
                }, null, SubscriptionCancelled);
            }

            /// <inheritdoc />
            protected override async ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                await CreateSignalRChannel().WithCancellation(cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc />
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _client.Reconnected -= OnClientReconnected;
                }
            }


            /// <inheritdoc />
            protected override ValueTask DisposeAsync(bool disposing) {
                Dispose(disposing);
                return default;
            }

        }
    }
}
