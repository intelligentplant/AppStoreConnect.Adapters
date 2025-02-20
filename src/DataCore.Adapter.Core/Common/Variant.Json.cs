using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DataCore.Adapter.Common {
    partial struct Variant {

        /// <summary>
        /// Serializes a value to a <see cref="JsonElement"/> and creates a new <see cref="Variant"/> 
        /// containing the serialized value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of value to serialize to JSON.
        /// </typeparam>
        /// <param name="value">
        ///   The value to serialize to JSON.
        /// </param>
        /// <param name="options">
        ///   The JSON serializer options to use when serializing the value.
        /// </param>
        /// <returns>
        ///   A new <see cref="Variant"/> containing the serialized JSON value.
        /// </returns>
        /// <remarks>
        ///   Serialization is performed using <see cref="JsonSerializer"/>. If <typeparamref name="T"/> 
        ///   is <see cref="JsonElement"/> the <paramref name="value"/> will be passed through 
        ///   as-is.
        /// </remarks>
#pragma warning disable RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
        public static Variant FromJsonValue<T>(T value, JsonSerializerOptions? options = null) => new Variant(value is JsonElement json ? json : JsonSerializer.SerializeToElement(value, options));
#pragma warning restore RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads


        /// <summary>
        /// Serializes a value to a <see cref="JsonElement"/> and creates a new <see cref="Variant"/> 
        /// containing the serialized value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of value to serialize to JSON.
        /// </typeparam>
        /// <param name="value">
        ///   The value to serialize to JSON.
        /// </param>
        /// <param name="jsonTypeInfo">
        ///   The JSON type information to use when serializing the value.
        /// </param>
        /// <returns>
        ///   A new <see cref="Variant"/> containing the serialized JSON value.
        /// </returns>
        /// <remarks>
        ///   Serialization is performed using <see cref="JsonSerializer"/>. If <typeparamref name="T"/> 
        ///   is <see cref="JsonElement"/> the <paramref name="value"/> will be passed through 
        ///   as-is.
        /// </remarks>
        public static Variant FromJsonValue<T>(T value, JsonTypeInfo<T> jsonTypeInfo) => new Variant(value is JsonElement json ? json : JsonSerializer.SerializeToElement(value, jsonTypeInfo));

    }

}
