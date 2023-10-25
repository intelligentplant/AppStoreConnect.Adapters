using System;

using FASTER.core;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Options for <see cref="FasterKeyValueStore"/>.
    /// </summary>
    public class FasterKeyValueStoreOptions : Services.KeyValueStoreOptions {

        /// <summary>
        /// Specifies if the <see cref="FasterKeyValueStore"/> is read-only.
        /// </summary>
        /// <remarks>
        ///   When <see langword="true"/>, write and delete operations will throw exceptions, 
        ///   compaction will not be performed, and no periodic checkpoints will be persisted, 
        ///   regardless of the <see cref="CompactionInterval"/>, <see cref="CheckpointManagerFactory"/> 
        ///   and <see cref="CheckpointInterval"/> properties.
        /// </remarks>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// When <see langword="true"/>, enables the use of <see cref="Services.IRawKeyValueStore.WriteRawAsync"/> 
        /// to write raw byte data to the store.
        /// </summary>
        /// <remarks>
        ///   Attempting a raw write will throw an exception if this property is <see langword="false"/>.
        /// </remarks>
        public bool EnableRawWrites { get; set; }

        /// <summary>
        /// A factory for creating an <see cref="IDevice"/> for use with the FASTER log.
        /// </summary>
        /// <remarks>
        ///   Specify <see langword="null"/> to create a default device using <see cref="Devices.CreateLogDevice"/> 
        ///   in a temporary location.
        /// </remarks>
        public Func<IDevice>? LogDeviceFactory { get; set; }

        /// <summary>
        /// A factory for creating an <see cref="ICheckpointManager"/> for use with the FASTER log.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   Use <see cref="FasterKeyValueStore.CreateLocalStorageCheckpointManager(string, bool)"/> 
        ///   to create an <see cref="ICheckpointManager"/> that will use a local directory to 
        ///   store checkpoint data.
        /// </para>
        /// 
        /// <para>
        ///   Unlike <see cref="LogDeviceFactory"/>, checkpoints will be disabled if no 
        ///   <see cref="CheckpointManagerFactory"/> delegate is provided, meaning that the 
        ///   store's data will not persist if the store is disposed. 
        /// </para>
        /// 
        /// <para>
        ///   <see cref="CheckpointInterval"/> controls how frequently automatic checkpoints are 
        ///   created. If an <see cref="ICheckpointManager"/> is supplied to the <see cref="FasterKeyValueStore"/>, 
        ///   a full snapshot checkpoint will always be attempted when the store is disposed.
        /// </para>
        /// 
        /// </remarks>
        public Func<ICheckpointManager>? CheckpointManagerFactory { get; set; }

        /// <summary>
        /// The number of buckets in the FASTER index.
        /// </summary>
        /// <remarks>
        ///   Each bucket is 64 bits in length.
        /// </remarks>
        public long IndexBucketCount { get; set; } = 1L << 20; // i.e. 1 x 1024 x 1024 = 1 MB; 1 MB x 64 bits = 64 MB index

        /// <summary>
        /// FASTER page size, in bits.
        /// </summary>
        /// <remarks>
        ///   A FASTER page is a contiguous block of in-memory or on-disk storage.
        /// </remarks>
        public int PageSizeBits { get; set; } = 25; // i.e. 2^25 = 33.5 MB

        /// <summary>
        /// Size, in bits, of the in-memory region of a FASTER log.
        /// </summary>
        /// <remarks>
        ///   If the log exceeds this size, the additional log entries will be written 
        ///   to on-disk storage.
        /// </remarks>
        public int MemorySizeBits { get; set; } = 26; // i.e. 2^26 = 67 MB

        /// <summary>
        /// Size, in bits, of a FASTER log segment in the on-disk region of the log.
        /// </summary>
        public int SegmentSizeBits { get; set; } = 27;

        /// <summary>
        /// The interval at which log checkpoints will be saved to <see cref="ICheckpointManager"/> 
        /// created by <see cref="CheckpointManagerFactory"/>.
        /// </summary>
        /// <remarks>
        ///   Note that checkpoints will not be created if no <see cref="ICheckpointManager"/> is 
        ///   supplied by <see cref="CheckpointManagerFactory"/>, or if <see cref="CheckpointInterval"/> 
        ///   is <see langword="null"/> or less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </remarks>
        public TimeSpan? CheckpointInterval { get; set; }

        /// <summary>
        /// The interval at which the store will check if the log should be compacted by 
        /// removing expired items.
        /// </summary>
        public TimeSpan? CompactionInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The size, in bytes, that the read-only part of the FASTER log must reach in order 
        /// to be compacted at the next compaction interval.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   As values are updated or removed, the previous versions of those items are moved to 
        ///   a read-only part of the FASTER log. These items remain in FASTER until the log is 
        ///   compacted. Compaction is performed on a periodic basis via the <see cref="CompactionInterval"/> 
        ///   setting. At each compaction interval, the FASTER log will be compacted if it is 
        ///   created than the current size threshold, in order to remove expired records from the 
        ///   log.
        /// </para>
        /// 
        /// <para>
        ///   This property controls the initial threshold only; the threshold will be increased 
        ///   if the log is being continually compacted but is still greater than the threshold 
        ///   (since this indicates that the log has now outgrown the original threshold).
        /// </para>
        /// 
        /// <para>
        ///   A value of less than or equal to zero will result in an initial compaction 
        ///   threshold that is twice the size of the in-memory portion of the log.
        /// </para>
        /// 
        /// </remarks>
        public long CompactionThresholdBytes { get; set; } = 0;

    }
}
