using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a custom property associated with an adapter, tag, tag value, event message, etc.
    /// </summary>
    [JsonConverter(typeof(AdapterPropertyConverter))]
    public class AdapterProperty {

        /// <summary>
        /// The property name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The property value.
        /// </summary>
        public Variant Value { get; }

        /// <summary>
        /// The property description.
        /// </summary>
        public string? Description { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public AdapterProperty(string name, Variant value, string? description = null) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            Description = description;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty Create(string name, Variant value, string? description = null) {
            return new AdapterProperty(name, value, description);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty Create(string name, object value, string? description = null) {
            return new AdapterProperty(name, Variant.FromValue(value), description);
        }


        /// <summary>
        /// Creates a copy of an existing <see cref="AdapterProperty"/>.
        /// </summary>
        /// <param name="property">
        ///   The property to clone.
        /// </param>
        /// <returns>
        ///   A copy of the property.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty FromExisting(AdapterProperty property) {
            if (property == null) {
                throw new ArgumentNullException(nameof(property));
            }

            return Create(property.Name, property.Value, property.Description);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="AdapterProperty"/>.
    /// </summary>
    internal class AdapterPropertyConverter : AdapterJsonConverter<AdapterProperty> {

        /// <inheritdoc/>
        public override AdapterProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null!;
            Variant value = Variant.Null;
            string description = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterProperty.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterProperty.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterProperty.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return AdapterProperty.Create(name, value, description);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterProperty value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AdapterProperty.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AdapterProperty.Value), value.Value, options);
            WritePropertyValue(writer, nameof(AdapterProperty.Description), value.Description, options);

            writer.WriteEndObject();
        }

    }
}
