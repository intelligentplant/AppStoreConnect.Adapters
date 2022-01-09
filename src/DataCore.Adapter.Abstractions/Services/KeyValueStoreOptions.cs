using System.IO.Compression;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Options for <see cref="KeyValueStore"/>.
    /// </summary>
    public class KeyValueStoreOptions {

        /// <summary>
        /// The compression level to use for data written to the store.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.NoCompression;

    }

}
