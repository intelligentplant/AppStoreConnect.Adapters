using System;
using System.Text.Json;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// <see cref="IFasterKeyValueStoreSerializer"/> that uses <see cref="JsonSerializer"/> to serialize 
    /// and deserialize values.
    /// </summary>
    public class JsonFasterKeyValueStoreSerializer : IFasterKeyValueStoreSerializer {

        /// <summary>
        /// JSON options.
        /// </summary>
        private readonly JsonSerializerOptions _options;


        /// <summary>
        /// Creates a new <see cref="JsonFasterKeyValueStoreSerializer"/> object.
        /// </summary>
        /// <param name="options">
        ///   The JSON options to use with the serializer.
        /// </param>
        public JsonFasterKeyValueStoreSerializer(JsonSerializerOptions? options = null) {
            _options = options ?? new JsonSerializerOptions();
            // Ensure that the converters for adapter types have been registered.
            _options.AddDataCoreAdapterConverters();
        }


        /// <inheritdoc/>
        public byte[] Serialize<TValue>(TValue? item) {
            return JsonSerializer.SerializeToUtf8Bytes(item, _options);
        }


        /// <inheritdoc/>
        public TValue? Deserialize<TValue>(ReadOnlySpan<byte> bytes) {
            return JsonSerializer.Deserialize<TValue>(bytes, _options);
        }

    }
}
