using System;
using System.IO.Compression;

namespace DataCore.Adapter.KeyValueStore.FileSystem {

    /// <summary>
    /// Options for <see cref="FileSystemKeyValueStore"/>.
    /// </summary>
    public class FileSystemKeyValueStoreOptions : Services.KeyValueStoreOptions {

        /// <summary>
        /// Default path to save files to.
        /// </summary>
        public const string DefaultPath = "./KeyValueStore";

        /// <summary>
        /// Default number of hash buckets.
        /// </summary>
        public const int DefaultHashBuckets = 20;

        /// <summary>
        /// The base path for the store's files.
        /// </summary>
        /// <remarks>
        ///   Relative paths will be made absolute relative to <see cref="AppContext.BaseDirectory"/>.
        /// </remarks>
        public string Path { get; set; } = DefaultPath;

        /// <summary>
        /// The number of hash buckets to distribute files across.
        /// </summary>
        public int HashBuckets { get; set; } = DefaultHashBuckets;


        /// <summary>
        /// Creates a new <see cref="FileSystemKeyValueStoreOptions"/> object.
        /// </summary>
        public FileSystemKeyValueStoreOptions() {
            CompressionLevel = CompressionLevel.NoCompression;
        }

    }

}
