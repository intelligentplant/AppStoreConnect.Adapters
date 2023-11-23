using System;

using DataCore.Adapter.Services;

using FASTER.core;

namespace DataCore.Adapter.KeyValueStore.FASTER {
    /// <summary>
    /// A record in the FASTER store.
    /// </summary>
    public readonly struct FasterRecord {

        /// <summary>
        /// The key.
        /// </summary>
        public KVKey Key { get; }

        /// <summary>
        /// The metadata for the record.
        /// </summary>
        public RecordMetadata Metadata { get; }

        /// <summary>
        /// Specifies if the record is located in the mutable portion of the FASTER log.
        /// </summary>
        public bool Mutable { get; }

        /// <summary>
        /// The value for the record.
        /// </summary>
        public ReadOnlyMemory<byte> Value { get; }


        /// <summary>
        /// Creates a new <see cref="FasterRecord"/> instance
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="metadata">
        ///   The metadata for the record.
        /// </param>
        /// <param name="mutable">
        ///   Specifies if the record is located in the mutable portion of the FASTER log.
        /// </param>
        /// <param name="value">
        ///   The record value.
        /// </param>
        internal FasterRecord(KVKey key, RecordMetadata metadata, bool mutable, ReadOnlyMemory<byte> value) {
            Key = key;
            Metadata = metadata;
            Mutable = mutable;
            Value = value;
        }

    }
}
