using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Holds information about a subscription channel.
    /// </summary>
    internal class SubscriptionChannel<TIdentifier, TTopic, TValue> : IDisposable {

        /// <summary>
        /// Indicates if the subscription has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The next value that will be published. Ignored if the <see cref="PublishInterval"/> 
        /// is less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        private TValue _nextPublishedValue;

        /// <summary>
        /// Indicates if a publish is pending.
        /// </summary>
        private int _publishPending;

        /// <summary>
        /// Subscription ID.
        /// </summary>
        public TIdentifier Id { get; }

        /// <summary>
        /// The context for the subscriber.
        /// </summary>
        public IAdapterCallContext Context { get; }

        /// <summary>
        /// The private publish channel. Values will be published to this channel and then 
        /// re-published to the public channel in a background task.
        /// </summary>
        private readonly Channel<(TValue Value, bool Immediate)> _inChannel;

        /// <summary>
        /// The public publish channel.
        /// </summary>
        private readonly Channel<TValue> _outChannel;

        /// <summary>
        /// The reader for the publish channel.
        /// </summary>
        public ChannelReader<TValue> Reader => _outChannel;

        /// <summary>
        /// The subscription topic.
        /// </summary>
        public TTopic Topic { get; }

        /// <summary>
        /// The publish interval. A value less than or equal to <see cref="TimeSpan.Zero"/> 
        /// will publish values immediately.
        /// </summary>
        public TimeSpan PublishInterval { get; }

        /// <summary>
        /// Cancellation token source that fires when the subscription is cancelled.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Fires when the subscription is cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// An action to perform when the subscription is cancelled or disposed.
        /// </summary>
        private readonly Action _cleanup;

        /// <summary>
        /// Registeration of <see cref="_cleanup"/> with <see cref="_cancellationTokenSource"/>.
        /// </summary>
        private readonly IDisposable _ctRegistration;


        /// <summary>
        /// Creates a new <see cref="SubscriptionChannel{TIdentifier, TTopic, TValue}"/> object.
        /// </summary>
        /// <param name="id">
        ///   The subscription ID.
        /// </param>
        /// <param name="context">
        ///   The context for the subscriber.
        /// </param>
        /// <param name="scheduler">
        ///   The task scheduler, used to run publish operations in a background task if required.
        /// </param>
        /// <param name="topic">
        ///   The topic to subscribe to.
        /// </param>
        /// <param name="publishInterval">
        ///   The publish interval for the subscription. When greater than <see cref="TimeSpan.Zero"/>, 
        ///   a background task will be used to periodically publish the last-received message. 
        ///   Otherwise, messages will be published immediately.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A set of cancellation tokens that will be observed in order to detect 
        ///   cancellation of the subscription.
        /// </param>
        /// <param name="cleanup">
        ///   An action that will be invoked when the subscription is cancelled or disposed.
        /// </param>
        /// <param name="channelCapacity">
        ///   The capacity of the output channel. A value less than or equal to zero specifies 
        ///   that an unbounded channel will be used. When a bounded channel is used, 
        ///   <see cref="BoundedChannelFullMode.DropWrite"/> is used as the behaviour when 
        ///   writing to a full channel.
        /// </param>
        public SubscriptionChannel(
            TIdentifier id,
            IAdapterCallContext context,
            IBackgroundTaskService scheduler,
            TTopic topic,
            TimeSpan publishInterval,
            CancellationToken[] cancellationTokens,
            Action cleanup,
            int channelCapacity = 0
        ) {
            Id = id;
            Context = context;

            // _inChannel is where incoming values are initially written to. They are then re-published 
            // to _outChannel by a dedicated background task.
            _inChannel = channelCapacity <= 0
                ? Channel.CreateUnbounded<(TValue, bool)>(new UnboundedChannelOptions() {
                    SingleReader = true,
                    SingleWriter = false
                })
                : Channel.CreateBounded<(TValue, bool)>(new BoundedChannelOptions(channelCapacity) {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropWrite
                });

            // This is the actual channel exposed via the Reader property.
            _outChannel = Channel.CreateUnbounded<TValue>(new UnboundedChannelOptions() {
                SingleReader = true,
                SingleWriter = true,
            });

            Topic = topic;
            PublishInterval = publishInterval;

            _cancellationTokenSource = cancellationTokens == null || cancellationTokens.Length == 0
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
            CancellationToken = _cancellationTokenSource.Token;

            _cleanup = () => {
                _inChannel.Writer.TryComplete();
                _outChannel.Writer.TryComplete();
                cleanup?.Invoke();
            };
            _ctRegistration = CancellationToken.Register(_cleanup);

            scheduler.QueueBackgroundWorkItem(RunIngressLoop, CancellationToken);

            // If we have a publish interval, run a background task to handle this.
            if (PublishInterval > TimeSpan.Zero) {
                scheduler.QueueBackgroundWorkItem(RunEgressLoop, CancellationToken);
            }
        }


        /// <summary>
        /// Publishes a value to the subscription.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="immediate">
        ///   When <see langword="true"/>, the value will be sent to the <see cref="Reader"/> 
        ///   immediately, even if the subscription is using a publish interval.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the value was successfully published, or 
        ///   <see langword="false"/> otherwise. A value of <see langword="false"/> can 
        ///   indicate that the channel is currently full, or it has been completed.
        /// </returns>
        public bool Publish(TValue value, bool immediate = false) {
            return _inChannel.Writer.TryWrite((value, immediate));
        }


        /// <summary>
        /// Continuously read values published via <see cref="Publish"/> and either re-
        /// publishes them to the <see cref="_outChannel"/> immediately or sets the next value 
        /// to be published by <see cref="RunEgressLoop"/> (if a publish interval is being 
        /// used).
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the task.
        /// </param>
        /// <returns>
        ///   A task that will run until the <paramref name="cancellationToken"/> requests 
        ///   cancellaion.
        /// </returns>
        private async Task RunIngressLoop(CancellationToken cancellationToken) {
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    try {
                        var val = await _inChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                        if (val.Immediate || PublishInterval <= TimeSpan.Zero) {
                            if (PublishInterval > TimeSpan.Zero) {
                                // Cancel next publish if one is already pending.
                                _publishPending = 0;
                                _nextPublishedValue = default;
                            }
                            await _outChannel.Writer.WriteAsync(val.Value, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        _nextPublishedValue = val.Value;
                        _publishPending = 1;
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                }
            }
            catch (Exception e) {
                _outChannel.Writer.TryComplete(e);
            }
            finally {
                _inChannel.Writer.TryComplete();
                _outChannel.Writer.TryComplete();
            }
        }


        /// <summary>
        /// Publishes values to the <see cref="_outChannel"/> on a periodic basis until 
        /// cancellation is requested. This method is only called if a publish interval is 
        /// specified for the subscription. 
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the task.
        /// </param>
        /// <returns>
        ///   A task that will run until the <paramref name="cancellationToken"/> requests 
        ///   cancellaion.
        /// </returns>
        private async Task RunEgressLoop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                await Task.Delay(PublishInterval, cancellationToken).ConfigureAwait(false);
                var publishPending = Interlocked.Exchange(ref _publishPending, 0);
                if (publishPending == 0) {
                    continue;
                }
                var val = _nextPublishedValue;
                if (val == null) {
                    continue;
                }
                await _outChannel.Writer.WriteAsync(val, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _ctRegistration.Dispose();
            if (!CancellationToken.IsCancellationRequested) {
                // Cancellation token source has not fired yet. Since we disposed of the 
                // registration for the cleanup callback above, we'll manually call it here, to 
                // ensure that cleanup occurs.
                _cleanup.Invoke();
            }
            _cancellationTokenSource.Dispose();

            _isDisposed = true;
        }
    }
}
