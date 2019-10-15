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
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The channel for the subscription.
        /// </summary>
        private readonly Channel<T> _channel;

        /// <summary>
        /// Cancellation token source that fires when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <inheritdoc/>
        public ChannelReader<T> Reader { get { return _channel; } }

        /// <summary>
        /// The writer for the subscription's channel.
        /// </summary>
        protected ChannelWriter<T> Writer { get { return _channel; } }

        /// <summary>
        /// A cancellation token that will fire when the subscription is disposed.
        /// </summary>
        protected CancellationToken SubscriptionCancelled {
            get { return _disposedTokenSource.Token; }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterSubscription{T}"/> obejct.
        /// </summary>
        protected AdapterSubscription() {
#pragma warning disable CA2214 // Do not call overridable methods in constructors
            _channel = CreateChannel();
#pragma warning restore CA2214 // Do not call overridable methods in constructors
        }


        /// <summary>
        /// Creates the <see cref="Channel{T}"/> used by the subscription.
        /// </summary>
        /// <returns>
        ///   A new <see cref="Channel{T}"/>. The channel will be closed automatically when the 
        ///   subscription is disposed.
        /// </returns>
        protected virtual Channel<T> CreateChannel() {
            return Channel.CreateUnbounded<T>();
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _channel.Writer.TryComplete();

            Dispose(true);

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Releases managed and unmanaged resources used by the subscription.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the subscription is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected abstract void Dispose(bool disposing);


        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            if (_isDisposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _channel.Writer.TryComplete();

            await DisposeAsync(true).ConfigureAwait(false);

            _isDisposed = true;
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }


        /// <summary>
        /// Releases managed and unmanaged resources used by the subscription.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the subscription is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will release the resources.
        /// </returns>
        protected abstract ValueTask DisposeAsync(bool disposing);


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~AdapterSubscription() {
            Dispose(false);
        }

    }
}
