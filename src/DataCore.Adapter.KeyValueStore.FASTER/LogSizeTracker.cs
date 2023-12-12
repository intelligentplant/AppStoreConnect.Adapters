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
        /// Actual memory used by the log (not including heap objects).
        /// </summary>
        public long MemorySizeBytes => _log.MemorySizeBytes;

        /// <summary>
        /// The size of the log's heap objects.
        /// </summary>
        public int HeapSizeBytes => _heapSize;

        /// <summary>
        /// The total size of the log and heap.
        /// </summary>
        public long TotalMemorySizeBytes => MemorySizeBytes + HeapSizeBytes;


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

                // If the record has not been deleted (i.e. it has been replaced by an upsert
                // operation), we need to account for the size of the value that was replaced in
                // addition to the size of the key.
                //
                // If the record is being deleted then the size of the evicted value is already
                // reported by SizeTrackingSpanByteFunctions.ConcurrentDeleter so we only need to
                // deduct the size of the evicted key here.

                if (!recordInfo.Tombstone) {
                    size += value.TotalSize;
                }
            }

            UpdateHeapSize(-size);
        }


        /// <summary>
        /// Updates the known heap size of the <see cref="LogAccessor{Key, Value}"/>.
        /// </summary>
        /// <param name="delta">
        ///   The change in heap size, in bytes.
        /// </param>
        internal void UpdateHeapSize(int delta) {
            if (delta == 0) {
                return;
            }
            Interlocked.Add(ref _heapSize, delta);
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
