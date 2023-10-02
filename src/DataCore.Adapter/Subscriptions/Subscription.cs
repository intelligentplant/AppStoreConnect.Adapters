using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Subscriptions {

    /// <summary>
    /// A subscriber to one or more topic streams produced by a <see cref="SubscriptionManager{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The subscription message type.
    /// </typeparam>
    /// <remarks>
    ///   New <see cref="Subscription{T}"/> instances are created by calling 
    ///   <see cref="SubscriptionManager{T}.CreateSubscription(string?)"/>.
    /// </remarks>
    public sealed class Subscription<T> : IDisposable {

        /// <summary>
        /// Specifies if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The subscription manager that created the subscription.
        /// </summary>
        private readonly SubscriptionManager<T> _subscriptionManager;

        /// <summary>
        /// The subscription ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The channel used to emit values to <see cref="ReadAllAsync"/>.
        /// </summary>
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions() { 
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        /// <summary>
        /// The topics that the subscription is subscribed to.
        /// </summary>
        /// <remarks>
        ///   <see cref="Topics"/> is <c>internal</c> so that it is accessible to 
        ///   <see cref="SubscriptionManager{T}"/>; it is unsafe to access it from any other code!
        /// </remarks>
        internal HashSet<SubscriptionTopic> Topics { get; } = new HashSet<SubscriptionTopic>();

        /// <summary>
        /// Lock used to synchronize access to <see cref="Topics"/>.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _topicsLock = new Nito.AsyncEx.AsyncReaderWriterLock();


        /// <summary>
        /// Creates a new <see cref="Subscription{T}"/> instance.
        /// </summary>
        /// <param name="subscriptionManager">
        ///   The subscription manager that created the subscription.
        /// </param>
        /// <param name="id">
        ///   The subscription ID. If <see langword="null"/> or whitespace, a new ID will be 
        ///   generated.
        /// </param>
        internal Subscription(SubscriptionManager<T> subscriptionManager, string? id) {
            _subscriptionManager = subscriptionManager;
            Id = string.IsNullOrWhiteSpace(id) 
                ? Guid.NewGuid().ToString()
                : id!;
        }


        /// <summary>
        /// Reads all values from the subscription.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token to use for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that can be used to read messages that are 
        ///   published to the subscription.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The object has been disposed.
        /// </exception>
        public async IAsyncEnumerable<T> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _subscriptionManager.LifetimeToken)) {
                await foreach (var item in _channel.Reader.ReadAllAsync(ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }


        /// <summary>
        /// Sends a message to the subscription.
        /// </summary>
        /// <param name="topic">
        ///   The topic that the message is associated with.
        /// </param>
        /// <param name="message">
        ///   The message to send.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to use for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return <see langword="true"/> if the 
        ///   <paramref name="message"/> was accepted, or <see langword="false"/> if the subscription 
        ///   is not subscribed to the <paramref name="topic"/>.
        /// </returns>
        /// <remarks>
        ///   <see cref="SubscriptionManager{T}"/> will call this method if it determines that the 
        ///   <see cref="Subscription{T}"/> is a candidate for receiving a message based on the 
        ///   topic hashes of the message topic and the subscribed topics.
        /// </remarks>
        public async ValueTask<bool> PublishAsync(string topic, T message, CancellationToken cancellationToken = default) {
            if (topic == null) {
                throw new ArgumentNullException(nameof(topic));
            }
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            
            if (_disposed) {
                return false;
            }
            
            using (await _topicsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                foreach (var t in Topics) {
                    if (SubscriptionTopicFilterComparer.Compare(topic, t.Topic) == SubscriptionTopicFilterCompareResult.IsMatch) {
                        return TryPublishCore(message);
                    }
                }
            }

            return false;
        }


        internal bool TryPublishCore(T message) {
            return _channel.Writer.TryWrite(message);
        }


        /// <summary>
        /// Subscribes to the specified topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to use for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription topic was added, or <see langword="false"/> 
        ///   if a subscription for the same topic already exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="topic"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has been disposed.
        /// </exception>
        public async ValueTask<bool> SubscribeAsync(string topic, CancellationToken cancellationToken = default) {
            if (topic == null) {
                throw new ArgumentNullException(nameof(topic));
            }
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var subscriptionTopic = new SubscriptionTopic(topic, _subscriptionManager.Options);

            using (await _topicsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                if (!Topics.Add(subscriptionTopic)) {
                    return false;
                }

                await _subscriptionManager.SubscribeAsync(this, subscriptionTopic, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }


        /// <summary>
        /// Unsubscribes from the specified topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic to unsubscribe from.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to use for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription topic was removed, or <see langword="false"/> 
        ///   if no subscription for the topic was present.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="topic"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has been disposed.
        /// </exception>
        public async ValueTask<bool> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var subscriptionTopic = new SubscriptionTopic(topic, _subscriptionManager.Options);

            using (await _topicsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                if (!Topics.Remove(subscriptionTopic)) {
                    return false;
                }

                await _subscriptionManager.UnsubscribeAsync(this, subscriptionTopic, cancellationToken).ConfigureAwait(false);

                return true;
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _channel.Writer.TryComplete();

            using (_topicsLock.WriterLock()) {
                _subscriptionManager.OnDisposed(this);
                Topics.Clear();
            }

            _disposed = true;
        }

    }
}
