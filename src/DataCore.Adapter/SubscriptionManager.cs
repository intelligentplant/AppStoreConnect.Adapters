using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class that can be used to manage subscriptions for push notification channels.
    /// </summary>
    /// <typeparam name="TOptions">
    ///   The options type for the subscription manager.
    /// </typeparam>
    /// <typeparam name="TTopic">
    ///   The type used to define the topic for a channel.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///   The value type for the notification channel.
    /// </typeparam>
    /// <typeparam name="TSubscription">
    ///   The subscription type.
    /// </typeparam>
    public abstract class SubscriptionManager<TOptions, TTopic, TValue, TSubscription> 
        : IBackgroundTaskServiceProvider, IFeatureHealthCheck, IDisposable 
        where TOptions : SubscriptionManagerOptions, new() 
        where TSubscription : SubscriptionChannel<TTopic, TValue> {

        /// <summary>
        /// The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </summary>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// The ID of the adapter associated with the subscription manager.
        /// </summary>
        public string? AdapterId { get; }

        /// <summary>
        /// Logging.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A cancellation token that will fire when the object is disposed.
        /// </summary>
        protected CancellationToken DisposedToken => _disposedTokenSource.Token;

        /// <summary>
        /// The subscription manager options.
        /// </summary>
        protected TOptions Options { get; }

        /// <summary>
        /// Maximum number of concurrent subscriptions.
        /// </summary>
        private readonly int _maxSubscriptionCount;

        /// <summary>
        /// The last subscription ID that was issued.
        /// </summary>
        private int _lastSubscriptionId;

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<int, TSubscription> _subscriptions = new ConcurrentDictionary<int, TSubscription>();

        /// <summary>
        /// Indicates if the subscription manager currently holds any subscriptions.
        /// </summary>
        protected bool HasSubscriptions { get { return !_subscriptions.IsEmpty; } }

        /// <summary>
        /// Publishes all event messages passed to the <see cref="SubscriptionManager{TOptions, TTopic, TValue, TSubscription}"/> 
        /// via the <see cref="ValueReceived"/> method.
        /// </summary>
        public event Action<TValue>? Publish;

        /// <summary>
        /// Raised whenever a subscription is added to the subscription manager.
        /// </summary>
        public event Action<TSubscription>? SubscriptionAdded;

        /// <summary>
        /// Raised whenever a subscription is cancelled.
        /// </summary>
        public event Action<TSubscription>? SubscriptionCancelled;

        /// <summary>
        /// Channel that is used to publish new event messages. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<(TValue Value, SubscriptionChannel<TTopic, TValue>[] Subscribers)> _masterChannel = Channel.CreateUnbounded<(TValue, SubscriptionChannel<TTopic, TValue>[])>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });


        /// <summary>
        /// Creates a new <see cref="SubscriptionManager{TOptions, TTopic, TValue, TSubscription}"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The backgrounnd task service to use when running background operations.
        /// </param>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        protected SubscriptionManager(TOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger) {
            Options = options ?? new TOptions();
            AdapterId = Options.AdapterId;
            _maxSubscriptionCount = Options.MaxSubscriptionCount;
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            BackgroundTaskService.QueueBackgroundWorkItem(
                PublishToSubscribers, 
                _disposedTokenSource.Token
            );
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        protected virtual void OnSubscriptionAdded(TSubscription subscription) {
            SubscriptionAdded?.Invoke(subscription);
        }


        /// <summary>
        /// Invoked when a subscription is removed.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        protected virtual void OnSubscriptionCancelled(TSubscription subscription) {
            SubscriptionCancelled?.Invoke(subscription);
        }


        /// <summary>
        /// Creates a new subscription channel.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="id">
        ///   The ID for the new subscription.
        /// </param>
        /// <param name="channelCapacity">
        ///   The capacity of the subscription channel.
        /// </param>
        /// <param name="cancellationTokens">
        ///   The cancellation tokens that the new subscription must obey.
        /// </param>
        /// <param name="cleanup">
        ///   A callback that must be invoked when the subscription is cancelled.
        /// </param>
        /// <param name="state">
        ///   The state value passed to <see cref="CreateSubscription"/>.
        /// </param>
        /// <returns>
        ///   A new <typeparamref name="TSubscription"/> that will emit values to the subscriber.
        /// </returns>
        protected abstract TSubscription CreateSubscriptionChannel(
            IAdapterCallContext context, 
            int id, 
            int channelCapacity,
            CancellationToken[] cancellationTokens,
            Action cleanup,
            object? state
        );


        /// <summary>
        /// Tests if a value published to the subscription manager matches any of the specified topics.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="topics">
        ///   The topics.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value matches any of the topics, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        protected virtual bool IsTopicMatch(TValue value, IEnumerable<TTopic> topics) {
            return true;
        }


        /// <summary>
        /// Creates a subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> describing the subscriber.
        /// </param>
        /// <param name="name">
        ///   A display name for the subscription.
        /// </param>
        /// <param name="state">
        ///   A state value that will be passed to <see cref="CreateSubscriptionChannel"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription provided by the caller.
        /// </param>
        /// <returns>
        ///   A new <typeparamref name="TSubscription"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The subscription manager does not have the capacity to add a new subscription.
        /// </exception>
        protected TSubscription CreateSubscription(IAdapterCallContext context, string? name, object? state, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (_maxSubscriptionCount > 0 && _subscriptions.Count >= _maxSubscriptionCount) {
                throw new InvalidOperationException(Resources.Error_TooManySubscriptions);
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = CreateSubscriptionChannel(
                context, 
                subscriptionId,
                Options.ChannelCapacity,
                new[] { DisposedToken, cancellationToken }, 
                () => OnSubscriptionCancelledInternal(subscriptionId), 
                state
            );
            _subscriptions[subscriptionId] = subscription;

            OnSubscriptionAdded(subscription);

            return subscription;
        }


        /// <summary>
        /// Creates a subscription associated with an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> describing the subscriber.
        /// </param>
        /// <param name="name">
        ///   The name of the feature method that is creating the subscription.
        /// </param>
        /// <param name="state">
        ///   A state value that will be passed to <see cref="CreateSubscriptionChannel"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription provided by the caller.
        /// </param>
        /// <returns>
        ///   A new <typeparamref name="TSubscription"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The subscription manager does not have the capacity to add a new subscription.
        /// </exception>
        protected TSubscription CreateSubscription<TFeature>(IAdapterCallContext context, string name, object? state, CancellationToken cancellationToken) where TFeature : IAdapterFeature {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(name));
            }
            if (_maxSubscriptionCount > 0 && _subscriptions.Count >= _maxSubscriptionCount) {
                throw new InvalidOperationException(Resources.Error_TooManySubscriptions);
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = CreateSubscriptionChannel(
                context,
                subscriptionId,
                Options.ChannelCapacity,
                new[] { DisposedToken, cancellationToken },
                () => OnSubscriptionCancelledInternal(subscriptionId),
                state
            );
            _subscriptions[subscriptionId] = subscription;

            OnSubscriptionAdded(subscription);

            return subscription;
        }


        /// <summary>
        /// Gets the active subscriptions.
        /// </summary>
        /// <returns>
        ///   The subscriptions.
        /// </returns>
        protected IEnumerable<TSubscription> GetSubscriptions() {
            return _subscriptions.Values.ToArray();
        }


        /// <summary>
        /// Notifies that a subscription was cancelled.
        /// </summary>
        /// <param name="id">
        ///   The cancelled subscription ID.
        /// </param>
        private void OnSubscriptionCancelledInternal(int id) {
            if (IsDisposed) {
                return;
            }

            if (_subscriptions.TryRemove(id, out var subscription)) {
                subscription.Dispose();
                OnSubscriptionCancelled(subscription);
            }
        }


        /// <summary>
        /// Publishes a message to subscribers.
        /// </summary>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the value was published to subscribers.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The subscription manager has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        public virtual async ValueTask<bool> ValueReceived(TValue message, CancellationToken cancellationToken = default) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var subscribers = _subscriptions.Values.Where(x => IsTopicMatch(message, x.Topics)).ToArray();

            try {
                using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                    await _masterChannel.Writer.WaitToWriteAsync(ctSource.Token).ConfigureAwait(false);
                    return _masterChannel.Writer.TryWrite((message, subscribers));
                }
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested) {
                    // Cancellation token provided by the caller has fired; rethrow the exception.
                    throw;
                }

                // The stream manager is being disposed.
                return false;
            }
        }


        /// <summary>
        /// Gets the properties to include in a call to <see cref="CheckFeatureHealthAsync"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <returns>
        ///   A dictionary of properties to include in the health check result.
        /// </returns>
        protected virtual IDictionary<string, string> GetHealthCheckProperties(IAdapterCallContext context) {
            var subscriptions = _subscriptions.Values.ToArray();

            return new Dictionary<string, string>() {
                [Resources.HealthChecks_Data_SubscriberCount] = subscriptions.Length.ToString(context?.CultureInfo)
            };
        }


        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var result = HealthCheckResult.Healthy(GetType().Name, data: GetHealthCheckProperties(context));

            return Task.FromResult(result);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~SubscriptionManager() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="SubscriptionManager{TOptions, TTopic, TValue, TSubscription}"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (IsDisposed) {
                return;
            }

            if (disposing) {
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _masterChannel.Writer.TryComplete();

                foreach (var item in _subscriptions.Values.ToArray()) {
                    item.Dispose();
                }

                _subscriptions.Clear();
            }

            IsDisposed = true;
        }


        /// <summary>
        /// Long-running task that sends event messages to subscribers whenever they are added to 
        /// the <see cref="_masterChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires.
        /// </returns>
        private async Task PublishToSubscribers(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    if (!await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        break;
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }

                while (_masterChannel.Reader.TryRead(out var item)) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    Publish?.Invoke(item.Value);
                    if (item.Subscribers.Length == 0) {
                        continue;
                    }

                    foreach (var subscriber in item.Subscribers) {
                        if (cancellationToken.IsCancellationRequested) {
                            break;
                        }

                        try {
                            var success = subscriber.Publish(item.Value);
                            if (!success) {
                                Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                            }
                        }
                        catch (Exception e) {
                            Logger.LogError(e, Resources.Log_PublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// Options for <see cref="SubscriptionManager{TOptions, TTopic, TValue, TSubscription}"/>.
    /// </summary>
    public class SubscriptionManagerOptions {

        /// <summary>
        /// The ID of the adapter associated with the subscription manager.
        /// </summary>
        public string? AdapterId { get; set; }

        /// <summary>
        /// The maximum number of concurrent subscriptions allowed. When this limit is hit, 
        /// attempts to create additional subscriptions will throw exceptions. A value less than 
        /// one indicates no limit.
        /// </summary>
        public int MaxSubscriptionCount { get; set; }

        /// <summary>
        /// The capacity of channels that publish items to subscribers. When a channel is at 
        /// capacity, attempts to write additional values into the channel will fail. A value 
        /// less than one indicates no limit.
        /// </summary>
        public int ChannelCapacity { get; set; } = 100;

    }

}
