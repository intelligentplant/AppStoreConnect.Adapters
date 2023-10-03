using System.IO.Compression;
using System.Text.Json;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Options for <see cref="KeyValueStore{TOptions}"/>.
    /// </summary>
    public class KeyValueStoreOptions {

        /// <summary>
        /// The JSON serializer options to use when serializing/deserializing values.
        /// </summary>
        public JsonSerializerOptions? JsonOptions { get; set; }

        /// <summary>
        /// The compression level to use for data written to the store.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.NoCompression;

    }

}
