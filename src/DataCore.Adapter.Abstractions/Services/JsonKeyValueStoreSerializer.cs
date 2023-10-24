using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IKeyValueStoreSerializer"/> implementation that uses <see cref="JsonSerializer"/>.
    /// </summary>
    public sealed class JsonKeyValueStoreSerializer : IKeyValueStoreSerializer {

        /// <summary>
        /// The JSON serializer options.
        /// </summary>
        private readonly JsonSerializerOptions? _jsonOptions;


        /// <summary>
        /// The <see cref="JsonKeyValueStoreSerializer"/> instance.
        /// </summary>
        public static IKeyValueStoreSerializer Default { get; } = new JsonKeyValueStoreSerializer(null);


        /// <summary>
        /// Creates a new <see cref="JsonKeyValueStoreSerializer"/> instance.
        /// </summary>
        /// <param name="jsonOptions"></param>
        public JsonKeyValueStoreSerializer(JsonSerializerOptions? jsonOptions) {
            _jsonOptions = jsonOptions;
        }


        /// <inheritdoc/>
        public async ValueTask SerializeAsync<T>(Stream stream, T value) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            await JsonSerializer.SerializeAsync(stream, value, _jsonOptions).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async ValueTask<T?> DeserializeAsync<T>(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions).ConfigureAwait(false);
        }

    }
}
