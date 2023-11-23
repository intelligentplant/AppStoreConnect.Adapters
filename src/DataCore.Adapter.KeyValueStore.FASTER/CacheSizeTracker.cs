using System;

using FASTER.core;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Tracks memory usage in a <see cref="FasterKV{Key, Value}"/>.
    /// </summary>
    internal class CacheSizeTracker : IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The underlying FASTER store.
        /// </summary>
        private readonly FasterKV<SpanByte, SpanByte> _store;

        /// <summary>
        /// The size tracker for the FASTER log.
        /// </summary>
        private readonly LogSizeTracker _logSizeTracker;

        /// <summary>
        /// The size tracker for the FASTER read cache.
        /// </summary>
        private readonly LogSizeTracker? _readCacheSizeTracker;


        /// <summary>
        /// Creates a new <see cref="CacheSizeTracker"/> instance.
        /// </summary>
        /// <param name="store">
        ///   The underlying FASTER store.
        /// </param>
        public CacheSizeTracker(FasterKV<SpanByte, SpanByte> store) {
            _store = store;

            _logSizeTracker = new LogSizeTracker(_store.Log);
            if (_store.ReadCache != null) {
                _readCacheSizeTracker = new LogSizeTracker(_store.ReadCache);
            }
        }


        /// <summary>
        /// Gets the total in-memory size of the FASTER store.
        /// </summary>
        /// <returns>
        ///   The total in-memory size of the FASTER store, in bytes.
        /// </returns>
        internal long GetTotalSize() => GetIndexSize() + GetLogSize() + GetReadCacheSize();

        /// <summary>
        /// Gets the size of the FASTER index.
        /// </summary>
        /// <returns>
        ///   The size of the FASTER index, in bytes.
        /// </returns>
        internal long GetIndexSize() => (_store.IndexSize * 64) + (_store.OverflowBucketCount * 64);

        /// <summary>
        /// Gets the size of the in-memory portion of the FASTER log.
        /// </summary>
        /// <returns>
        ///   The size of the in-memory portion of the FASTER log, in bytes.
        /// </returns>
        internal long GetLogSize() => _logSizeTracker.TotalMemorySize;

        /// <summary>
        /// Gets the size of the FASTER read cache.
        /// </summary>
        /// <returns>
        ///   The size of the FASTER read cache, in bytes.
        /// </returns>
        internal long GetReadCacheSize() => _readCacheSizeTracker?.TotalMemorySize ?? 0;


        /// <summary>
        /// Updates the heap size of the FASTER store.
        /// </summary>
        /// <param name="delta">
        ///   The change in heap size.
        /// </param>
        /// <param name="isReadCache">
        ///   <see langword="true"/> if the change is for the read cache; otherwise, <see langword="false"/>.
        /// </param>
        internal void UpdateHeapSize(int delta, bool isReadCache = false) {
            if (isReadCache) {
                _readCacheSizeTracker?.UpdateHeapSize(delta);
            }
            else {
                _logSizeTracker.UpdateHeapSize(delta);
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _logSizeTracker.Dispose();
            _readCacheSizeTracker?.Dispose();

            _disposed = true;
        }
    }

}
