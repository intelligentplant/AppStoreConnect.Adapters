using System;
using System.Text.Json;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// Extension methods for working with <see cref="JsonElement"/>.
    /// </summary>
    public static class JsonElementExtensions {

        /// <summary>
        /// Serializes the specified <paramref name="value"/> to a <see cref="JsonElement"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="value">
        ///   The value to serialize.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <returns>
        ///   A <see cref="JsonElement"/> representing the serialized <paramref name="value"/>.
        /// </returns>
        public static JsonElement ToJsonElement<T>(this T? value, JsonSerializerOptions? options = null) {
            return DeserializeJsonFromUtf8Bytes((ReadOnlyMemory<byte>) JsonSerializer.SerializeToUtf8Bytes(value, options)) ?? default;
        }


        /// <summary>
        /// Deserializes the bytes to a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="jsonBytes">
        ///   The JSON string to deserialize, encoded as UTF-8 bytes.
        /// </param>
        /// <returns>
        ///   A <see cref="JsonElement"/> representing the deserialized bytes, or <see langword="null"/> 
        ///   if <paramref name="jsonBytes"/> has zero length.
        /// </returns>
        public static JsonElement? DeserializeJsonFromUtf8Bytes(this ReadOnlyMemory<byte> jsonBytes) {
            if (jsonBytes.Length == 0) {
                return default;
            }
            return JsonDocument.Parse(jsonBytes).RootElement;
        }


        /// <summary>
        /// Deserializes the bytes to a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="jsonBytes">
        ///   The JSON string to deserialize, encoded as UTF-8 bytes.
        /// </param>
        /// <returns>
        ///   A <see cref="JsonElement"/> representing the deserialized bytes, or <see langword="null"/> 
        ///   if <paramref name="jsonBytes"/> is <see langword="null"/> or has zero length.
        /// </returns>
        public static JsonElement? DeserializeJsonFromUtf8Bytes(this byte[] jsonBytes) {
            if (jsonBytes?.Length == 0) {
                return default;
            }
            return JsonDocument.Parse(jsonBytes).RootElement;
        }


        /// <summary>
        /// Deserializes the <see cref="JsonElement"/> to an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to deserialize the <see cref="JsonElement"/> to.
        /// </typeparam>
        /// <param name="json">
        ///   The <see cref="JsonElement"/> to deserialize.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <returns>
        ///   An instance of <typeparamref name="T"/>.
        /// </returns>
        public static T? Deserialize<T>(this JsonElement json, JsonSerializerOptions? options = null) {
            return JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(json, options), options);
        }


        /// <summary>
        /// Serializes the <see cref="JsonElement"/> to a JSON string, encoded as UTF-8 bytes.
        /// </summary>
        /// <param name="json">
        ///   The <see cref="JsonElement"/> to serialize.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <returns>
        ///   The serialized JSON, encoded as UTF-8 bytes.
        /// </returns>
        public static byte[] SerializeToUtf8Bytes(this JsonElement json, JsonSerializerOptions? options = null) {
            if (json.ValueKind == JsonValueKind.Undefined) {
                return Array.Empty<byte>();
            }

            return JsonSerializer.SerializeToUtf8Bytes(json, options);
        }

    }
}
