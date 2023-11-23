using System;
using System.Threading;

using FASTER.core;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Tracks memory usage in a FASTER <see cref="LogAccessor{Key, Value}"/>.
    /// </summary>
    internal class LogSizeTracker : IObserver<IFasterScanIterator<SpanByte, SpanByte>>, IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The <see cref="LogAccessor{Key, Value}"/> to track.
        /// </summary>
        private readonly LogAccessor<SpanByte, SpanByte> _log;

        /// <summary>
        /// The subscription that receives eviction notifications from the <see cref="_log"/>.
        /// </summary>
        private readonly IDisposable _subscription;

        /// <summary>
        /// The size of the heap allocated by the <see cref="_log"/>.
        /// </summary>
        private int _heapSize;

        /// <summary>
        /// The number of records in the heap for the <see cref="_log"/>.
        /// </summary>
        private int _recordCount;

        /// <summary>
        /// The total size of the <see cref="_log"/> and heap.
        /// </summary>
        public long TotalMemorySize => _log.MemorySizeBytes + _heapSize;

        /// <summary>
        /// The number of records in the heap for the <see cref="LogAccessor{Key, Value}"/>.
        /// </summary>
        public int RecordCount => _recordCount;


        /// <summary>
        /// Creates a new <see cref="LogSizeTracker"/> instance.
        /// </summary>
        /// <param name="log">
        ///   The <see cref="LogAccessor{Key, Value}"/> to track.
        /// </param>
        public LogSizeTracker(LogAccessor<SpanByte, SpanByte> log) {
            _log = log;
            _subscription = _log.SubscribeEvictions(this);
        }


        /// <inheritdoc/>
        public void OnCompleted() {
            // No-op
        }


        /// <inheritdoc/>
        public void OnError(Exception error) {
            // No-op
        }


        /// <inheritdoc/>
        public void OnNext(IFasterScanIterator<SpanByte, SpanByte> iterator) {
            // We are only subscribed to be notified when items are evicted from the log, so we
            // will always decrement the tracked log size.

            var size = 0;

            while (iterator.GetNext(out var recordInfo, out var key, out var value)) {
                size += key.TotalSize;
                if (!recordInfo.Tombstone) {
                    // The record has not been deleted (e.g. it has been evicted and replaced
                    // due to an update), so we need to account for the value size that was
                    // replaced.
                    size += value.TotalSize;
                }
            }

            Interlocked.Add(ref _heapSize, -size);
        }


        /// <summary>
        /// Updates the known heap size of the <see cref="LogAccessor{Key, Value}"/>.
        /// </summary>
        /// <param name="delta">
        ///   The change in heap size, in bytes.
        /// </param>
        internal void UpdateHeapSize(int delta) {
            Interlocked.Add(ref _heapSize, delta);
            // If the delta is positive, we have added a new record to the heap; otherwise, we
            // have removed a record.
            if (delta > 0) {
                Interlocked.Increment(ref _recordCount);
            }
            else {
                Interlocked.Decrement(ref _recordCount);
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _subscription.Dispose();
            _disposed = true;
        }

    }

}
