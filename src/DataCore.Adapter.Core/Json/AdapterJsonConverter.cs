using System;
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
    internal abstract class AdapterJsonConverter<T> : JsonConverter<T> {

        /// <summary>
        /// A flag indicating if <typeparamref name="T"/> is serliazed/deserialized as a JSON 
        /// object.
        /// </summary>
        protected virtual bool SerializeAsObject { get; set; } = true;


        /// <summary>
        /// Throws a <see cref="JsonException"/> to indicate that the JSON structure is invalid.
        /// </summary>
        protected void ThrowInvalidJsonError() {
            throw new JsonException(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_InvalidJsonStructure, typeof(T).Name));
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
        protected string ConvertPropertyName(string name, JsonSerializerOptions? options) {
            return options?.PropertyNamingPolicy?.ConvertName(name) ?? name;
        }


        /// <summary>
        /// Writes a property name and value to the specified JSON writer.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="propertyName">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <param name="options">
        ///   The JSON options.
        /// </param>
        protected void WritePropertyValue<TValue>(Utf8JsonWriter writer, string propertyName, TValue value, JsonSerializerOptions options) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WritePropertyName(ConvertPropertyName(propertyName, options));
            JsonSerializer.Serialize(writer, value, options);
        }


        /// <summary>
        /// Writes a property name and value to the specified JSON writer.
        /// </summary>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="propertyName">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <param name="inputType">
        ///   The value type.
        /// </param>
        /// <param name="options">
        ///   The JSON options.
        /// </param>
        protected void WritePropertyValue(Utf8JsonWriter writer, string propertyName, object? value, Type inputType, JsonSerializerOptions options) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WritePropertyName(ConvertPropertyName(propertyName, options));
            JsonSerializer.Serialize(writer, value, inputType, options);
        }


        /// <summary>
        /// Writes a property name with a null value to the specified JSON writer.
        /// </summary>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="propertyName">
        ///   The property name.
        /// </param>
        /// <param name="options">
        ///   The JSON options.
        /// </param>
        protected void WriteNullPropertyValue(Utf8JsonWriter writer, string propertyName, JsonSerializerOptions options) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WritePropertyName(ConvertPropertyName(propertyName, options));
            writer.WriteNullValue();
        }

    }

}
