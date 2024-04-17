using FASTER.core;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Extends <see cref="SpanByteFunctions{Context}"/> to notify a <see cref="CacheSizeTracker"/> 
    /// instance about upserts, deletes and copies to the FASTER read cache.
    /// </summary>
    internal sealed class SizeTrackingSpanByteFunctions : SpanByteFunctions<Empty> {

        /// <summary>
        /// The <see cref="CacheSizeTracker"/> that tracks memory usage.
        /// </summary>
        private readonly CacheSizeTracker _sizeTracker;


        /// <summary>
        /// Creates a new <see cref="SizeTrackingSpanByteFunctions"/> instance.
        /// </summary>
        /// <param name="sizeTracker">
        ///   The <see cref="CacheSizeTracker"/> that tracks memory usage.
        /// </param>
        public SizeTrackingSpanByteFunctions(CacheSizeTracker sizeTracker) {
            _sizeTracker = sizeTracker;
        }


        /// <inheritdoc/>
        public override bool ConcurrentWriter(ref SpanByte key, ref SpanByte input, ref SpanByte src, ref SpanByte dst, ref SpanByteAndMemory output, ref UpsertInfo upsertInfo) {
            var delta = src.TotalSize - dst.TotalSize;
            if (base.ConcurrentWriter(ref key, ref input, ref src, ref dst, ref output, ref upsertInfo)) {
                _sizeTracker.UpdateHeapSize(delta);
                return true;
            }

            return false;
        }


        /// <inheritdoc/>
        public override void PostSingleWriter(ref SpanByte key, ref SpanByte input, ref SpanByte src, ref SpanByte dst, ref SpanByteAndMemory output, ref UpsertInfo upsertInfo, WriteReason reason) {
            var delta = key.TotalSize + src.TotalSize;
            base.PostSingleWriter(ref key, ref input, ref src, ref dst, ref output, ref upsertInfo, reason);
            _sizeTracker.UpdateHeapSize(delta, reason == WriteReason.CopyToReadCache);
        }


        /// <inheritdoc/>
        public override bool ConcurrentDeleter(ref SpanByte key, ref SpanByte value, ref DeleteInfo deleteInfo) {
            var delta = value.TotalSize;
            if (base.ConcurrentDeleter(ref key, ref value, ref deleteInfo)) {
                _sizeTracker.UpdateHeapSize(-delta);
                if (deleteInfo.RecordInfo.Invalid) {
                    // Record was marked as invalid. FASTER example code indicates that this means
                    // that the record was not inserted, so deduct the size of the key from the
                    // heap as well.
                    _sizeTracker.UpdateHeapSize(-key.TotalSize);
                }
                return true;
            }

            return false;
        }

    }

}
