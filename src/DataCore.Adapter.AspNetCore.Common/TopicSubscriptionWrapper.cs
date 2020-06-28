using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Wraps an inner <see cref="IAdapterSubscriptionWithTopics{T}"/>, allowing creation of 
    /// separate channels for individual topics on the subscription.
    /// </summary>
    /// <typeparam name="T">
    ///   The value type for the subscription.
    /// </typeparam>
    public sealed class TopicSubscriptionWrapper<T> : IAdapterSubscriptionWithTopics<T> {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The inner subscription.
        /// </summary>
        private readonly IAdapterSubscriptionWithTopics<T> _inner;

        /// <summary>
        /// Subscribers to individual topics.
        /// </summary>
        private readonly List<TopicChannel<T>> _topicSubscribers = new List<TopicChannel<T>>();

        /// <summary>
        /// Lock for accessing <see cref="_topicSubscribers"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _topicSubscribersLock = new ReaderWriterLockSlim();
        
        /// <inheritdoc/>
        public string Id {
            get { return _inner.Id; }
        }

        /// <inheritdoc/>
        public bool IsStarted {
            get { return _inner.IsStarted; }
        }

        /// <inheritdoc/>
        public IAdapterCallContext Context {
            get { return _inner.Context; }
        }

        /// <inheritdoc/>
        public CancellationToken CancellationToken {
            get { return _inner.CancellationToken; }
        }

        /// <inheritdoc/>
        public Task Completed {
            get { return _inner.Completed; }
        }

        /// <summary>
        /// A channel reader that will emit items published to the subscription. This property 
        /// value is always null on a <see cref="TopicSubscriptionWrapper{T}"/>. Use the 
        /// <see cref="CreateTopicChannel"/> method to create a channel that will emit values for 
        /// a specific topic.
        /// </summary>
        ChannelReader<T> IAdapterSubscription<T>.Reader {
            get { return null; }
        }


        /// <summary>
        /// Creates a new <see cref="TopicSubscriptionWrapper{T}"/> object.
        /// </summary>
        /// <param name="inner">
        ///   The inner subscription to wrap.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running the background task 
        ///   that will process values emitted from the inner subscription.
        /// </param>
        public TopicSubscriptionWrapper(IAdapterSubscriptionWithTopics<T> inner, IBackgroundTaskService backgroundTaskService) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            (backgroundTaskService ?? BackgroundTaskService.Default).QueueBackgroundWorkItem(ProcessSubscriptionChannel, CancellationToken);
        }


        /// <inheritdoc/>
        public void Cancel() {
            _inner.Cancel();
        }


        /// <summary>
        /// Subscribes to the specified topic. The return value for this method on <see cref="TopicSubscriptionWrapper{T}"/> 
        /// is always <see langword="false"/>. Use the <see cref="CreateTopicChannel"/> method to 
        /// create a channel that will emit values for a specific topic and then dispose of it 
        /// when the topic subscription is no longer required.
        /// </summary>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        ValueTask<bool> IAdapterSubscriptionWithTopics<T>.SubscribeToTopic(string topic) {
            return new ValueTask<bool>(false);
        }


        /// <summary>
        /// Unsubscribes from the specified topic. The return value for this method on <see cref="TopicSubscriptionWrapper{T}"/> 
        /// is always <see langword="false"/>. Use the <see cref="CreateTopicChannel"/> method to 
        /// create a channel that will emit values for a specific topic and then dispose of it 
        /// when the topic subscription is no longer required.
        /// </summary>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        ValueTask<bool> IAdapterSubscriptionWithTopics<T>.UnsubscribeFromTopic(string topic) {
            return new ValueTask<bool>(false);
        }


        /// <inheritdoc/>
        public bool IsMatch(T value, string topic) {
            return _inner.IsMatch(value, topic);
        }


        /// <summary>
        /// Creates a channel that will emit values for the specified topic. The channel can be 
        /// disposed when it is no longer required.
        /// </summary>
        /// <param name="topic">
        ///   The topic to subscribe to.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a new <see cref="TopicChannel{T}"/>.
        /// </returns>
        public async Task<TopicChannel<T>> CreateTopicChannel(string topic) {
            if (string.IsNullOrWhiteSpace(topic)) {
                throw new ArgumentException(Resources.Error_SubscriptionTopicIsRequired, nameof(topic));
            }

            var result = new TopicChannel<T>(this, topic);

            var create = false;

            _topicSubscribersLock.EnterWriteLock();
            try {
                _topicSubscribers.Add(result);
                create = _topicSubscribers.Count(x => string.Equals(x.Topic, topic, StringComparison.OrdinalIgnoreCase)) == 1;
            }
            finally {
                _topicSubscribersLock.ExitWriteLock();
            }

            if (create) {
                var success = await _inner.SubscribeToTopic(topic).ConfigureAwait(false);
                if (!success) {
                    // Subscribe failed; dispose the result so that the writer completes.
                    await result.DisposeAsync().ConfigureAwait(false);
                }
            }

            return result;
        }


        /// <summary>
        /// Called when a <see cref="TopicChannel{T}"/> is disposed.
        /// </summary>
        /// <param name="topicChannel">
        ///   The disposed channel.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        internal async ValueTask OnTopicChannelDisposed(TopicChannel<T> topicChannel) {
            if (_isDisposed) {
                return;
            }

            var delete = false;

            _topicSubscribersLock.EnterWriteLock();
            try {
                if (_topicSubscribers.Remove(topicChannel)) {
                    // Unsubscribe from the topic in the inner subscription if there are no more 
                    // subscribers for this topic.
                    delete = !_topicSubscribers.Any(x => string.Equals(x.Topic, topicChannel.Topic, StringComparison.OrdinalIgnoreCase));
                }
            }
            finally {
                _topicSubscribersLock.ExitWriteLock();
            }

            if (delete) {
                await _inner.UnsubscribeFromTopic(topicChannel.Topic).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Long-running task that will read items from the inner subscription's channel and 
        /// republish them to <see cref="TopicChannel{T}"/> instances created by this wrapper.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the long-running operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will process values emitted by the inner subscription.
        /// </returns>
        private async Task ProcessSubscriptionChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    var val = await _inner.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    if (val == null) {
                        continue;
                    }

                    TopicChannel<T>[] subscribers;
                    _topicSubscribersLock.EnterReadLock();
                    try {
                        subscribers = _topicSubscribers.Where(x => IsMatch(val, x.Topic)).ToArray();
                        if (subscribers.Length == 0) {
                            continue;
                        }
                    }
                    finally {
                        _topicSubscribersLock.ExitReadLock();
                    }

                    foreach (var subscriber in subscribers) {
                        subscriber.Writer.TryWrite(val);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception) {
                    Dispose();
                    return;
                }
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            _inner.Dispose();
            _topicSubscribersLock.Dispose();
            _topicSubscribers.Clear();
        }
    }

}
