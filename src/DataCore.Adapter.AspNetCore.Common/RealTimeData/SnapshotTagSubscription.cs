using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.RealTimeData {
    /// <summary>
    /// A subscription to an individual tag created by a <see cref="SnapshotSubscriptionWrapper"/>.
    /// </summary>
    public sealed class SnapshotTagSubscription : IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private int _isDisposed;

        /// <summary>
        /// A callback that is invoked when the subscription is disposed.
        /// </summary>
        private readonly Action<SnapshotTagSubscription> _onDisposed;

        /// <summary>
        /// The tag ID for the subscription.
        /// </summary>
        public string TagId { get; }

        /// <summary>
        /// The channel that will emit tag values.
        /// </summary>
        private readonly Channel<TagValueQueryResult> _channel = Channel.CreateUnbounded<TagValueQueryResult>();

        /// <summary>
        /// The channel that will emit tag values.
        /// </summary>
        public ChannelReader<TagValueQueryResult> Reader { get { return _channel; } }


        /// <summary>
        /// Creates a new <see cref="SnapshotTagSubscription"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="onDisposed">
        ///   The callback to invoke when the subscription is disposed.
        /// </param>
        internal SnapshotTagSubscription(string tagId, Action<SnapshotTagSubscription> onDisposed) {
            TagId = tagId;
            _onDisposed = onDisposed;
        }


        /// <summary>
        /// Writes a value to the <see cref="Reader"/>.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return <see langword="true"/> if 
        ///   the value was written, or <see langword="false"/> otherwise.
        /// </returns>
        internal async ValueTask<bool> WriteAsync(TagValueQueryResult value, CancellationToken cancellationToken = default) {
            if (!await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                return false;
            }

            return _channel.Writer.TryWrite(value);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) {
                return;
            }

            _channel.Writer.TryComplete();
            _onDisposed?.Invoke(this);
        }

    }
}
