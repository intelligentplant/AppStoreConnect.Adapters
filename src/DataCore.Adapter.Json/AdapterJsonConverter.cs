using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// Base class when implementing a <see cref="JsonConverter{T}"/> for an adapter DTO.
    /// </summary>
    /// <typeparam name="T">
    ///   The object type for the converter.
    /// </typeparam>
    public abstract class AdapterJsonConverter<T> : JsonConverter<T> {

        /// <summary>
        /// A flag indicating if <typeparamref name="T"/> is serliazed/deserialized as a JSON 
        /// object.
        /// </summary>
        protected virtual bool SerializeAsObject { get; set; } = true;


        /// <summary>
        /// Throws a <see cref="JsonException"/> to indicate that the JSON structure is invalid.
        /// </summary>
        protected void ThrowInvalidJsonError() {
            throw new JsonException(string.Format(CultureInfo.CurrentCulture, Resources.Error_InvalidJsonStructure, typeof(T).Name));
        }


        /// <summary>
        /// Converts the specified property name using the naming policy specified in the JSON 
        /// serializer options.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="options">
        ///   The JSON serializer options.
        /// </param>
        /// <returns>
        ///   The converted property name.
        /// </returns>
        protected string ConvertPropertyName(string name, JsonSerializerOptions options) {
            return options?.ConvertPropertyName(name);
        }


        /// <summary>
        /// Reads an array of values from the specified JSON reader.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The array value type.
        /// </typeparam>
        /// <param name="reader">
        ///   The JSON reader.
        /// </param>
        /// <param name="options">
        ///   The JSON options.
        /// </param>
        /// <returns>
        ///   The array items that were read from the JSON reader.
        /// </returns>
        protected IEnumerable<TValue> ReadArrayValues<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            var result = new List<TValue>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                result.Add(JsonSerializer.Deserialize<TValue>(ref reader, options));
            }

            return result;
        }


        /// <summary>
        /// Writes a collection of values to the specified JSON writer.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The collection value type.
        /// </typeparam>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="values">
        ///   The values to write.
        /// </param>
        /// <param name="options">
        ///   The JSON options.
        /// </param>
        protected void WriteArrayValues<TValue>(Utf8JsonWriter writer, IEnumerable<TValue> values, JsonSerializerOptions options) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteStartArray();

            if (values != null) {
                foreach (var item in values) {
                    JsonSerializer.Serialize(writer, item, options);
                }
            }

            writer.WriteEndArray();
        }

    }

}
