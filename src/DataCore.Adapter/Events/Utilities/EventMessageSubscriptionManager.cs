using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Events.Utilities {

    /// <summary>
    /// Base class for simplifying implementation of the <see cref="IEventMessagePush"/> feature.
    /// </summary>
    public abstract class EventMessageSubscriptionManager : IEventMessagePush, IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();

        /// <summary>
        /// For protecting access to <see cref="_subscriptions"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Indicates if the subscription manager currently holds any subscriptions.
        /// </summary>
        protected bool HasSubscriptions { get; private set; }

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }

        /// <summary>
        /// Channel that is used to publish new event messages. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<EventMessage> _masterChannel = Channel.CreateUnbounded<EventMessage>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true
        });


        /// <summary>
        /// Creates a new <see cref="EventMessageSubscriptionManager"/> object.
        /// </summary>
        protected EventMessageSubscriptionManager() {
            _ = Task.Factory.StartNew(async () => {
                try {
                    await PublishToSubscribers(_disposedTokenSource.Token).ConfigureAwait(false);
                }
                catch { }
            }, TaskCreationOptions.LongRunning);
        }


        /// <inheritdoc/>
        public IEventMessageSubscription Subscribe(IAdapterCallContext context, bool active) {
            var subscription = new Subscription(this, active);

            bool added;
            lock (_subscriptions) {
                added = _subscriptions.Add(subscription);
                if (added) {
                    HasSubscriptions = _subscriptions.Count > 0;
                    HasActiveSubscriptions = _subscriptions.Any(x => x.IsActive);
                }
            }

            if (added) {
                OnSubscriptionAdded();
            }

            return subscription;
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        protected abstract void OnSubscriptionAdded();


        /// <summary>
        /// Invoked when a subscription is removed.
        /// </summary>
        protected abstract void OnSubscriptionRemoved();


        /// <summary>
        /// Sends an event message to subscribers.
        /// </summary>
        /// <param name="message"></param>
        protected void OnMessage(EventMessage message) {
            if (_isDisposed || _disposedTokenSource.IsCancellationRequested || message == null) {
                return;
            }
            _masterChannel.Writer.TryWrite(message);
        }


        /// <summary>
        /// Notifies the <see cref="EventMessageSubscriptionManager"/> that a subscription was disposed.
        /// </summary>
        /// <param name="subscription">
        ///   The disposed subscription.
        /// </param>
        private void OnSubscriptionDisposed(Subscription subscription) {
            bool removed;
            lock (_subscriptions) {
                removed = _subscriptions.Remove(subscription);
                if (removed) {
                    HasSubscriptions = _subscriptions.Count > 0;
                    HasActiveSubscriptions = _subscriptions.Any(x => x.IsActive);
                }
            }

            if (removed) {
                OnSubscriptionRemoved();
            }
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            DisposeInternal(true);

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~EventMessageSubscriptionManager() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="EventMessageSubscriptionManager"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        private void DisposeInternal(bool disposing) {
            try {
                Dispose(disposing);
            }
            finally {
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _masterChannel.Writer.TryComplete();
                _subscriptionsLock.EnterWriteLock();
                try {
                    foreach (var item in _subscriptions.ToArray()) {
                        item.Dispose();
                    }
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }
                _subscriptionsLock.Dispose();
                _subscriptions.Clear();
            }
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="EventMessageSubscriptionManager"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected abstract void Dispose(bool disposing);


        /// <summary>
        /// Long-running task that sends tag values to subscribers whenever they are added to the 
        /// <see cref="_masterChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires
        /// </returns>
        private async Task PublishToSubscribers(CancellationToken cancellationToken) {
            try {
                while (await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (_masterChannel.Reader.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        Subscription[] subscribers;

                        _subscriptionsLock.EnterReadLock();
                        try {
                            subscribers = _subscriptions.ToArray();
                        }
                        finally {
                            _subscriptionsLock.ExitReadLock();
                        }

                        await Task.WhenAll(subscribers.Select(x => x.OnMessageReceived(message))).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancellation token fired
            }
            catch (ChannelClosedException) {
                // Channel was closed
            }
        }


        /// <summary>
        /// <see cref="IEventMessageSubscription"/> implementation.
        /// </summary>
        private class Subscription : IEventMessageSubscription {

            /// <summary>
            /// Flags if the object has been disposed.
            /// </summary>
            private bool _isDisposed;

            /// <summary>
            /// Fires when then object is being disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

            /// <summary>
            /// The subscription manager that the subscription is attached to.
            /// </summary>
            private readonly EventMessageSubscriptionManager _subscriptionManager;

            /// <summary>
            /// The channel that event messages will be written to.
            /// </summary>
            private readonly Channel<EventMessage> _channel;

            /// <inheritdoc/>
            public ChannelReader<EventMessage> Reader {
                get { return _channel.Reader; }
            }

            /// <summary>
            /// Indicates if the subscription is an active or passive event listener.
            /// </summary>
            internal bool IsActive { get; }


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="subscriptionManager">
            ///   The subscription manager that the subscription is attached to.
            /// </param>
            /// <param name="active">
            ///   Indicates if the subscription is an active or passive event listener.
            /// </param>
            internal Subscription(EventMessageSubscriptionManager subscriptionManager, bool active) {
                _subscriptionManager = subscriptionManager;
                _channel = Channel.CreateUnbounded<EventMessage>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                });
                IsActive = active;
            }


            /// <summary>
            /// Writes an event message to the subscription channel.
            /// </summary>
            /// <param name="message">
            ///   The event message.
            /// </param>
            /// <returns>
            ///   A task that will write the message to the event channel.
            /// </returns>
            internal async Task OnMessageReceived(EventMessage message) {
                if (_isDisposed || _disposedTokenSource.IsCancellationRequested || _channel.Reader.Completion.IsCompleted) {
                    return;
                }

                if (message == null) {
                    return;
                }

                await _channel.Writer.WriteAsync(message, _disposedTokenSource.Token).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                _channel.Writer.TryComplete();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _subscriptionManager.OnSubscriptionDisposed(this);
                _isDisposed = true;
            }

        }

    }
}
