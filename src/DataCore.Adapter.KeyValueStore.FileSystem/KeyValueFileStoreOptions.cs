using System;

namespace DataCore.Adapter.KeyValueStore.FileSystem {

    /// <summary>
    /// Options for <see cref="KeyValueFileStore"/>.
    /// </summary>
    public class KeyValueFileStoreOptions {

        /// <summary>
        /// Default path to save files to.
        /// </summary>
        public const string DefaultPath = "./KeyValueStore";

        /// <summary>
        /// The base path for the serialized JSON files.
        /// </summary>
        /// <remarks>
        ///   Relative paths will be made absolute relative to <see cref="AppContext.BaseDirectory"/>.
        /// </remarks>
        public string Path { get; set; } = DefaultPath;

    }

}
