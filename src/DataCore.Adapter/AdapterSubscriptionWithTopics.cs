using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Base implementation of <see cref="IAdapterSubscriptionWithTopics{T}"/>.
    /// </summary>
    /// <typeparam name="TValue">
    ///   The type of item that is emitted by the subscription.
    /// </typeparam>
    /// <typeparam name="TTopic">
    ///   The type that topic names will be resolved to.
    /// </typeparam>
    /// <remarks>
    ///   If your subscription does not need to resolve a topic name to an instance of a different 
    ///   type, consider inheriting from <see cref="AdapterSubscriptionWithTopics{T}"/> instead.
    /// </remarks>
    public abstract class AdapterSubscriptionWithTopics<TValue, TTopic> : AdapterSubscription<TValue>, IAdapterSubscriptionWithTopics<TValue> where TTopic : class {

        /// <summary>
        /// Channel that will publish changes to topic subscriptions.
        /// </summary>
        private readonly Channel<SubscriptionTopicChange> _subscriptionChangesChannel = Channel.CreateUnbounded<SubscriptionTopicChange>();

        /// <summary>
        /// The subscribed topics, indexed by topic name.
        /// </summary>
        private readonly Dictionary<string, TTopic> _subscribedTopics = new Dictionary<string, TTopic>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Lock for accessing <see cref="_subscribedTopics"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscribedTopicsLock = new ReaderWriterLockSlim();


        /// <summary>
        /// Creates a new <see cref="AdapterSubscriptionWithTopics{TValue, TTopic}"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        protected AdapterSubscriptionWithTopics(IAdapterCallContext context, string id) 
            : base(context, id) { }


        /// <inheritdoc/>
        public async ValueTask<bool> SubscribeToTopic(string topic) {
            if (CancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(topic) || !await _subscriptionChangesChannel.Writer.WaitToWriteAsync(CancellationToken).ConfigureAwait(false)) {
                return false;
            }

            var request = new SubscriptionTopicChange(new UpdateSubscriptionTopicsRequest() {
                Topic = topic,
                Action = Common.SubscriptionUpdateAction.Subscribe
            });

            return _subscriptionChangesChannel.Writer.TryWrite(request) && await request.Completed.ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async ValueTask<bool> UnsubscribeFromTopic(string topic) {
            if (CancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(topic) || !await _subscriptionChangesChannel.Writer.WaitToWriteAsync(CancellationToken).ConfigureAwait(false)) {
                return false;
            }

            var request = new SubscriptionTopicChange(new UpdateSubscriptionTopicsRequest() {
                Topic = topic,
                Action = Common.SubscriptionUpdateAction.Unsubscribe
            });

            return _subscriptionChangesChannel.Writer.TryWrite(request) && await request.Completed.ConfigureAwait(false);
        }


        /// <summary>
        /// Resolves a topic name to its <typeparamref name="TTopic"/> value.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that returns the equivalent <typeparamref name="TValue"/> 
        ///   value, or <see langword="null"/> if the topic name cannot be resolved.
        /// </returns>
        protected abstract ValueTask<TTopic> ResolveTopic(IAdapterCallContext context, string topic);


        /// <summary>
        /// Invoked when a topic is added to the subscription.
        /// </summary>
        /// <param name="topic">
        ///   The topic that was added.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        protected abstract Task OnTopicAdded(TTopic topic);


        /// <summary>
        /// Invoked when a topic is removed from the subscription.
        /// </summary>
        /// <param name="topic">
        ///   The topic that was removed.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        protected abstract Task OnTopicRemoved(TTopic topic);


        /// <summary>
        /// Gets the topic name for the the specified value. 
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   The topic name.
        /// </returns>
        protected abstract string GetTopicNameForValue(TValue value);


        /// <inheritdoc/>
        protected sealed override async Task Run(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
            OnRunning();
            await RunSubscription(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Starts a long-running task that will read subscription changes from the  
        /// channel until the channel closes or the provided cancellation token fires.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the long-running task.
        /// </param>
        /// <returns>
        ///   A long-running task that will run until the channel closes or the cancellation token 
        ///   fires.
        /// </returns>
        private async Task RunSubscription(
            CancellationToken cancellationToken
        ) {
            while (await _subscriptionChangesChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (_subscriptionChangesChannel.Reader.TryRead(out var change)) {
                    if (change == null) {
                        break;
                    }

                    TTopic resolvedTopic;

                    try {
                        switch (change.Request.Action) {
                            case Common.SubscriptionUpdateAction.Subscribe:
                                // Ask the subscription object to resolve this tag.
                                resolvedTopic = await ResolveTopic(Context, change.Request.Topic).ConfigureAwait(false);
                                if (cancellationToken.IsCancellationRequested || resolvedTopic == null) {
                                    change.SetResult(false);
                                    continue;
                                }
                                await AddTopicToSubscription(change.Request.Topic, resolvedTopic).ConfigureAwait(false);
                                break;
                            case Common.SubscriptionUpdateAction.Unsubscribe:
                                // Ensure that this change concerns a tag that we are actually subscribed to.
                                _subscribedTopicsLock.EnterReadLock();
                                try {
                                    _subscribedTopics.TryGetValue(change.Request.Topic, out resolvedTopic);
                                }
                                finally {
                                    _subscribedTopicsLock.ExitReadLock();
                                }
                                if (resolvedTopic == null) {
                                    change.SetResult(false);
                                    continue;
                                }
                                await RemoveTopicFromSubscription(change.Request.Topic, resolvedTopic).ConfigureAwait(false);
                                break;
                        }

                        change.SetResult(true);
                    }
                    catch {
                        change.SetResult(false);
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// Performs any required initialisation tasks when the subscription is started.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will perform any required initialisation tasks.
        /// </returns>
        protected virtual Task Init(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Gets all topics that have been added to the subscription.
        /// </summary>
        /// <returns>
        ///   A collection of <typeparamref name="TTopic"/> objects representing the subscribed topics.
        /// </returns>
        public IEnumerable<TTopic> GetSubscribedTopics() {
            _subscribedTopicsLock.EnterReadLock();
            try {
                return _subscribedTopics.Values.ToArray();
            }
            finally {
                _subscribedTopicsLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Tests if the subscription is subscribed to the specified topic name.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription is subscribed to the topic name, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        protected bool IsSubscribedToTopicName(string topic) {
            if (string.IsNullOrWhiteSpace(topic)) {
                return false;
            }

            _subscribedTopicsLock.EnterReadLock();
            try {
                return _subscribedTopics.ContainsKey(topic);
            }
            finally {
                _subscribedTopicsLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Tests if the subscription is subscribed to receive the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription can receive the value, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        ///   The default behaviour is to use <see cref="GetTopicNameForValue"/> get the topic 
        ///   name for the value, and to check if the topic name exactly matches any of the 
        ///   topic names being observed by the subscription.
        /// </remarks>
        public virtual bool IsSubscribed(TValue value) {
            if (value == null || CancellationToken.IsCancellationRequested) {
                return false;
            }

            var topicName = GetTopicNameForValue(value);
            return IsSubscribedToTopicName(topicName);
        }


        /// <summary>
        /// Adds a topic to the subscription.
        /// </summary>
        /// <param name="name">
        ///   The topic name.
        /// </param>
        /// <param name="topic">
        ///   The resolved topic to add.
        /// </param>
        /// <returns>
        ///   A task that will add the topic to the subscription.
        /// </returns>
        private async Task AddTopicToSubscription(string name, TTopic topic) {
            if (CancellationToken.IsCancellationRequested || topic == null) {
                throw new ArgumentNullException(nameof(topic));
            }

            _subscribedTopicsLock.EnterUpgradeableReadLock();
            try {
                if (_subscribedTopics.ContainsKey(name)) {
                    return;
                }
                _subscribedTopicsLock.EnterWriteLock();
                try {
                    _subscribedTopics[name] = topic;
                }
                finally {
                    _subscribedTopicsLock.ExitWriteLock();
                }
            }
            finally {
                _subscribedTopicsLock.ExitUpgradeableReadLock();
            }
            // Notify that the tag was added to the subscription.
            await OnTopicAdded(topic).ConfigureAwait(false);
        }


        /// <summary>
        /// Removes a topic from the subscription.
        /// </summary>
        /// <param name="name">
        ///   The topic name.
        /// </param>
        /// <param name="topic">
        ///   The topic to remove.
        /// </param>
        /// <returns>
        ///   A task that will remove the topic from the subscription.
        /// </returns>
        private async Task RemoveTopicFromSubscription(string name, TTopic topic) {
            CancellationToken.ThrowIfCancellationRequested();

            var removed = false;
            _subscribedTopicsLock.EnterWriteLock();
            try {
                removed = _subscribedTopics.Remove(name);
            }
            finally {
                _subscribedTopicsLock.ExitWriteLock();
            }

            if (removed) {
                // Notify that the tag was removed from the subscription.
                await OnTopicRemoved(topic).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override void OnCancelled() {
            _subscriptionChangesChannel.Writer.TryComplete();
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _subscribedTopics.Clear();
            _subscribedTopicsLock.Dispose();
        }

    }


    /// <summary>
    /// <see cref="AdapterSubscriptionWithTopics{TValue, TTopic}"/> implementation that does not 
    /// resolve topic names to a different type.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of item that is emitted by the subscription.
    /// </typeparam>
    public abstract class AdapterSubscriptionWithTopics<T> : AdapterSubscriptionWithTopics<T, string> {

        /// <summary>
        /// Creates a new <see cref="AdapterSubscriptionWithTopics{T}"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        protected AdapterSubscriptionWithTopics(IAdapterCallContext context, string id)
            : base(context, id) { }


        /// <inheritdoc/>
        protected sealed override ValueTask<string> ResolveTopic(IAdapterCallContext context, string topic) {
            return new ValueTask<string>(topic);
        }

    }

}
