﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for querying event messages, including pushing event messages to 
    // subscribers. Event message push is only supported on adapters that implement the 
    // IEventMessagePush feature.

    public partial class AdapterHub {

        #region [ OnConnected/OnDisconnected ]

        /// <summary>
        /// Invoked when a new connection is created.
        /// </summary>
        private void OnEventsHubConnected() {
            // Store a list of adapter subscriptions in the connection context.
            Context.Items[typeof(IEventMessageSubscription)] = new List<EventMessageSubscription>();
        }


        /// <summary>
        /// Invoked when a connection is closed.
        /// </summary>
        private void OnEventsHubDisconnected() {
            // Remove the adapter subscriptions from the connection context.
            if (Context.Items.TryGetValue(typeof(IEventMessageSubscription), out var o)) {
                Context.Items.Remove(typeof(IEventMessageSubscription));
                if (o is List<EventMessageSubscription> observers) {
                    foreach (var observer in observers.ToArray()) {
                        observer.Dispose();
                    }
                    observers.Clear();
                }
            }
        }

        #endregion

        #region [ Subscription Management ]

        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="subscriptionType">
        ///   Specifies if an active or passive subscription should be created.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageChannel(string adapterId, EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IEventMessagePush>(adapterId, cancellationToken).ConfigureAwait(false);
            var subscription = await GetOrAddEventMessageSubscription(AdapterCallContext, adapter.Adapter, adapter.Feature, subscriptionType, cancellationToken).ConfigureAwait(false);
            return subscription.Reader;
        }


        /// <summary>
        /// Gets or creates an event subscription to the specified adapter.
        /// </summary>
        /// <param name="callContext">
        ///   The call context.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="feature">
        ///   The event message push feature for the adapter.
        /// </param>
        /// <param name="subscriptionType">
        ///   Specifies if an active or passive subscription should be created.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   An <see cref="IEventMessageSubscription"/> for the adapter
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   <paramref name="adapter"/> does not support the <see cref="IEventMessagePush"/> feature.
        /// </exception>
        private async Task<IEventMessageSubscription> GetOrAddEventMessageSubscription(IAdapterCallContext callContext, IAdapter adapter, IEventMessagePush feature, EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            var subscription = GetEventMessageSubscription(adapter);
            if (subscription != null) {
                return subscription;
            }

            var subscriptionsForConnection = Context.Items[typeof(IEventMessageSubscription)] as List<EventMessageSubscription>;
            subscription = await feature.Subscribe(callContext, subscriptionType, cancellationToken).ConfigureAwait(false);

            EventMessageSubscription result;
            lock (subscriptionsForConnection) {
                result = new EventMessageSubscription(adapter.Descriptor.Id, subscription, subscriptionsForConnection, cancellationToken);
                subscriptionsForConnection.Add(result);
            }

            await result.StartAsync(callContext, cancellationToken).ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// Gets an existing event message subscription on the specified adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The <see cref="IEventMessageSubscription"/> for the specified adapter, or 
        ///   <see langword="null"/> if a subscription does not exist.
        /// </returns>
        private IEventMessageSubscription GetEventMessageSubscription(IAdapter adapter) {
            var subscriptionsForConnection = Context.Items[typeof(IEventMessageSubscription)] as List<EventMessageSubscription>;
            return subscriptionsForConnection?.FirstOrDefault(x => string.Equals(x.AdapterId, adapter.Descriptor.Id));
        }

        #endregion

        #region [ Polling Queries ]

        /// <summary>
        /// Reads event messages occurring inside the specified time range.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> ReadEventMessagesForTimeRange(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadEventMessagesForTimeRange>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadEventMessages(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Reads event messages starting at the specified cursor position.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessagesUsingCursor(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadEventMessagesUsingCursor>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadEventMessages(AdapterCallContext, request, cancellationToken);
        }

        #endregion

        #region [ Write Event Messages ]

        /// <summary>
        /// Writes event messages to the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide the event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the write results.
        /// </returns>
        public async Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(string adapterId, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IWriteEventMessages>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.WriteEventMessages(AdapterCallContext, channel, cancellationToken);
        }

        #endregion

        #region [ Inner Types ]

        /// <summary>
        /// Subscription wrapper class.
        /// </summary>
        private class EventMessageSubscription : IEventMessageSubscription {

            /// <summary>
            /// The adapter ID for the subscription.
            /// </summary>
            public string AdapterId { get; }

            /// <summary>
            /// The inner subscription returned by the adapter.
            /// </summary>
            private readonly IEventMessageSubscription _inner;

            /// <summary>
            /// Called when the subscription is disposed.
            /// </summary>
            private Action _onDisposed;

            /// <summary>
            /// Automatically disposes the subscription if the caller cancels the streaming request.
            /// </summary>
            private readonly CancellationTokenRegistration _onStreamCancelled;

            /// <inheritdoc/>
            bool IAdapterSubscription<EventMessage>.IsStarted {
                get { return _inner.IsStarted; }
            }

            /// <inheritdoc/>
            ChannelReader<EventMessage> IAdapterSubscription<EventMessage>.Reader {
                get { return _inner.Reader; }
            }


            /// <summary>
            /// Creates a new <see cref="EventMessageSubscription"/> object.
            /// </summary>
            /// <param name="adapterId">
            ///   The adapter ID.
            /// </param>
            /// <param name="inner">
            ///   The inner subscription returned by the adapter.
            /// </param>
            /// <param name="subscriptionsForConnection">
            ///   The subscriptions dictionary for the connection.
            /// </param>
            /// <param name="streamCancelled">
            ///   A cancellation token that will fire if the streaming request is cancelled by the caller.
            /// </param>
            internal EventMessageSubscription(string adapterId, IEventMessageSubscription inner, List<EventMessageSubscription> subscriptionsForConnection, CancellationToken streamCancelled) {
                AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _onDisposed = () => {
                    lock (subscriptionsForConnection) {
                        subscriptionsForConnection.Remove(this);
                    }
                };
                _onStreamCancelled = streamCancelled.Register(Dispose);
            }


            /// <inheritdoc/>
            public async ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                await _inner.StartAsync(context, cancellationToken).ConfigureAwait(false);
            } 


            /// <inheritdoc/>
            public void Dispose() {
                _onDisposed?.Invoke();
                _onDisposed = null;
                _onStreamCancelled.Dispose();
                _inner.Dispose();
            }


            /// <inheritdoc/>
            public async ValueTask DisposeAsync() {
                _onDisposed?.Invoke();
                _onDisposed = null;
                _onStreamCancelled.Dispose();
                await _inner.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion

    }
}
