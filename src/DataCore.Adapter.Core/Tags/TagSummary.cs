using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes summary information about a tag.
    /// </summary>
    [JsonConverter(typeof(TagSummaryConverter))]
    public class TagSummary : TagIdentifier {

        /// <summary>
        /// The tag description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The tag units.
        /// </summary>
        public string? Units { get; }

        /// <summary>
        /// The tag data type.
        /// </summary>
        public VariantType DataType { get; }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="description">
        ///   The tag description.
        /// </param>
        /// <param name="units">
        ///   The tag units.
        /// </param>
        /// <param name="dataType">
        ///   The tag data type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagSummary(string id, string name, string? description, string? units, VariantType dataType) 
            : base(id, name) {
            Description = description ?? string.Empty;
            Units = units ?? string.Empty;
            DataType = dataType;
        }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="description">
        ///   The tag description.
        /// </param>
        /// <param name="units">
        ///   The tag units.
        /// </param>
        /// <param name="dataType">
        ///   The tag data type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static TagSummary Create(string id, string name, string? description, string? units, VariantType dataType) {
            return new TagSummary(id, name, description, units, dataType);
        }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object that is a copy of an existing instance.
        /// </summary>
        /// <param name="other">
        ///   The <see cref="TagSummary"/> to copy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>
        ///   A new <see cref="TagSummary"/> instance.
        /// </returns>
        public static TagSummary FromExisting(TagSummary other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return Create(other.Id, other.Name, other.Description, other.Units, other.DataType);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagSummary"/>.
    /// </summary>
    internal class TagSummaryConverter : AdapterJsonConverter<TagSummary> {

        /// <inheritdoc/>
        public override TagSummary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            string description = null!;
            string units = null!;
            VariantType dataType = VariantType.Unknown;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagSummary.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagSummary.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagSummary.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagSummary.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagSummary.DataType), StringComparison.OrdinalIgnoreCase)) {
                    dataType = JsonSerializer.Deserialize<VariantType>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return TagSummary.Create(id, name, description, units, dataType);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagSummary value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagSummary.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagSummary.Name), value.Name, options);
            WritePropertyValue(writer, nameof(TagSummary.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagSummary.Units), value.Units, options);
            WritePropertyValue(writer, nameof(TagSummary.DataType), value.DataType, options);
            writer.WriteEndObject();
        }

    }

}
