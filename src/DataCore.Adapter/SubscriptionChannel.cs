using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Holds information about a subscription channel.
    /// </summary>
    public class SubscriptionChannel<TTopic, TValue> : IDisposable {

        /// <summary>
        /// Indicates if the subscription has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The next value that will be published. Ignored if the <see cref="PublishInterval"/> 
        /// is less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        private TValue _nextPublishedValue = default!;

        /// <summary>
        /// Indicates if a publish is pending.
        /// </summary>
        private int _publishPending;

        /// <summary>
        /// Subscription ID.
        /// </summary>
        public int Id { get; }

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
        /// The subscription topics.
        /// </summary>
        private readonly List<TTopic> _topics;

        /// <summary>
        /// The subscription topics.
        /// </summary>
        public IEnumerable<TTopic> Topics {
            get {
                lock (_topics) {
                    return _topics.ToArray();
                }
            }
        }

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
        /// Creates a new <see cref="SubscriptionChannel{TTopic, TValue}"/> object.
        /// </summary>
        /// <param name="id">
        ///   The subscription ID.
        /// </param>
        /// <param name="context">
        ///   The context for the subscriber.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The background task service, used to run publish operations in a background task if required.
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
            int id,
            IAdapterCallContext context,
            IBackgroundTaskService backgroundTaskService,
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
                SingleWriter = false,
            });

            _topics = new List<TTopic>();
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

            backgroundTaskService.QueueBackgroundWorkItem(RunIngressLoop, null, true, CancellationToken);

            // If we have a publish interval, run a background task to handle this.
            if (PublishInterval > TimeSpan.Zero) {
                backgroundTaskService.QueueBackgroundWorkItem(RunEgressLoop, null, true, CancellationToken);
            }
        }


        /// <summary>
        /// Adds topics to the subscription.
        /// </summary>
        /// <param name="topics">
        ///   The topics.
        /// </param>
        internal void AddTopics(IEnumerable<TTopic> topics) {
            lock (_topics) {
                _topics.AddRange(topics);
            }
        }


        /// <summary>
        /// Removes a topic from the subscription.
        /// </summary>
        /// <param name="topic">
        ///   The topics.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the topic was removed, or <see langword="false"/> if no matching topic was found.
        /// </returns>
        internal bool RemoveTopic(TTopic topic) {
            lock (_topics) {
                return _topics.Remove(topic);
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
                while (await _inChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    while (_inChannel.Reader.TryRead(out var val)) {
                        if (val.Immediate || PublishInterval <= TimeSpan.Zero) {
                            if (PublishInterval > TimeSpan.Zero) {
                                // Cancel next publish if one is already pending.
                                _publishPending = 0;
                                _nextPublishedValue = default!;
                            }
                            _outChannel.Writer.TryWrite(val.Value);
                            continue;
                        }

                        _nextPublishedValue = val.Value;
                        _publishPending = 1;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ChannelClosedException) { }
            catch (Exception e) {
                _inChannel.Writer.TryComplete(e);
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
            try {
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
            catch (OperationCanceledException) { 
                // Do nothing.
            }
            catch (Exception e) {
                _outChannel.Writer.TryComplete(e);
            }
            finally {
                _outChannel.Writer.TryComplete();
            }
        }


        /// <summary>
        /// Creates an <see cref="IAsyncEnumerable{T}"/> that enables reading all of the data 
        /// from the subscription.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the enumeration.
        /// </param>
        /// <returns>
        ///   A new <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        public async IAsyncEnumerable<TValue> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken) {
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken)) {
                await foreach (var item in _outChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~SubscriptionChannel() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the object is being disposed, or <see langword="false"/> 
        ///   it if is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _ctRegistration.Dispose();
                if (!CancellationToken.IsCancellationRequested) {
                    // Cancellation token source has not fired yet. Since we disposed of the 
                    // registration for the cleanup callback above, we'll manually call it here, to 
                    // ensure that cleanup occurs.
                    _cleanup.Invoke();
                }
                _cancellationTokenSource.Dispose();
            }

            _isDisposed = true;
        }

    }
}
