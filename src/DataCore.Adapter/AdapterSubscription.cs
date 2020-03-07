using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Base implementation of <see cref="IAdapterSubscription{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The subscription item type.
    /// </typeparam>
    public abstract class AdapterSubscription<T> : IAdapterSubscription<T> {

        /// <summary>
        /// A flag that specifies if the subscription has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Fires when the subscription is disposed.
        /// </summary>
        private readonly CancellationTokenSource _subscriptionCancelled = new CancellationTokenSource();

        /// <summary>
        /// Indicates if the subscription has been started.
        /// </summary>
        private int _isStarted;

        /// <summary>
        /// Indicates if the subscription has previously been started using the <see cref="Start"/> 
        /// method. Note that this property does not reset when the subscription is disposed.
        /// </summary>
        public bool IsStarted => _isStarted == 1;

        /// <summary>
        /// Channel that will publish received values.
        /// </summary>
        private readonly Channel<T> _valuesChannel = Channel.CreateUnbounded<T>();

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </summary>
        public IAdapterCallContext Context { get; }

        /// <summary>
        /// A channel that will publish the received values.
        /// </summary>
        public ChannelReader<T> Values => _valuesChannel;

        /// <summary>
        /// A cancellation token that will fire when the subscription disposed.
        /// </summary>
        public CancellationToken CancellationToken => _subscriptionCancelled.Token;

        /// <summary>
        /// Completes when the subscription is disposed.
        /// </summary>
        private readonly TaskCompletionSource<int> _completed = new TaskCompletionSource<int>();

        /// <summary>
        /// A task that will complete when the subscription is disposed.
        /// </summary>
        public Task Completed => _completed.Task;


        /// <summary>
        /// Creates a new <see cref="AdapterSubscription{T}"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        protected AdapterSubscription(IAdapterCallContext context) {
            Context = context;
        }


        /// <summary>
        /// Starts the subscription.
        /// </summary>
        public void Start() {
            if (IsDisposed || Interlocked.CompareExchange(ref _isStarted, 1, 0) != 0) {
                // Already started.
                return;
            }

            _ = Task.Run(async () => {
                try {
                    await Run(CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    _valuesChannel.Writer.TryComplete();
                }
                catch (ChannelClosedException) {
                    _valuesChannel.Writer.TryComplete();
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    _valuesChannel.Writer.TryComplete(e);
                }
                finally {
                    _valuesChannel.Writer.TryComplete();
                }
            });
        }


        /// <summary>
        /// Creates a long-running task that runs the subscription until the provided cancellation 
        /// token fires.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token to observe.
        /// </param>
        /// <returns>
        ///   A long-running task that will complete when the cancellation token fires.
        /// </returns>
        protected virtual Task Run(CancellationToken cancellationToken) {
            return Completed;
        }


        /// <summary>
        /// Publishes a value to the <see cref="Values"/> channel.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> that 
        ///   indicates if the value was published to the subscription.
        /// </returns>
        public async ValueTask<bool> ValueReceived(T value, CancellationToken cancellationToken = default) {
            if (IsDisposed || value == null) {
                return false;
            }

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken)) {
                if (await _valuesChannel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                    return _valuesChannel.Writer.TryWrite(value);
                }
            }

            return false;
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~AdapterSubscription() {
            Dispose(false);
        }


        /// <summary>
        /// Releases subscription resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the subscription is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (IsDisposed) {
                    return;
                }

                IsDisposed = true;
                _valuesChannel.Writer.TryComplete();
                _subscriptionCancelled.Cancel();
                _subscriptionCancelled.Dispose();
                _completed.TrySetResult(0);
            }
        }

    }
}
