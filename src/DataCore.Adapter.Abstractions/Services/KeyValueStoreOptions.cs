using System.IO.Compression;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Options for <see cref="KeyValueStore{TOptions}"/>.
    /// </summary>
    public class KeyValueStoreOptions {

        /// <summary>
        /// The serializer to use when serializing/deserializing values.
        /// </summary>
        public IKeyValueStoreSerializer? Serializer { get; set; }

        /// <summary>
        /// The compression level to use for data written to the store.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.NoCompression;

    }

}
