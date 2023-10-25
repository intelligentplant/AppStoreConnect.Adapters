using System.IO.Compression;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Base implementation of <see cref="IKeyValueStore"/>.
    /// </summary>
    /// <remarks>
    ///   Implementations should extend <see cref="KeyValueStore"/> or <see cref="KeyValueStore{TOptions}"/> 
    ///   rather than implementing <see cref="IKeyValueStore"/> directly.
    /// </remarks>
    /// <seealso cref="KeyValueStore"/>
    public abstract class KeyValueStore<TOptions> : KeyValueStore where TOptions : KeyValueStoreOptions, new() {

        /// <summary>
        /// The options for the store.
        /// </summary>
        protected TOptions Options { get; }


        /// <summary>
        /// Creates a new <see cref="KeyValueStore{TOptions}"/>.
        /// </summary>
        /// <param name="options">
        ///   Store options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        protected KeyValueStore(TOptions? options, ILogger? logger = null) : base(logger) {
            Options = options ?? new TOptions();
        }


        /// <inheritdoc/>
        protected sealed override CompressionLevel GetCompressionLevel() => Options.CompressionLevel;


        /// <inheritdoc/>
        protected sealed override IKeyValueStoreSerializer GetSerializer() => Options.Serializer ?? JsonKeyValueStoreSerializer.Default;

    }
}
