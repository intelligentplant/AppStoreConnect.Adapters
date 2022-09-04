using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a value returned by a tag value query.
    /// </summary>
    [JsonConverter(typeof(TagValueQueryResultConverter))]
    public class TagValueQueryResult : TagDataContainer {

        /// <summary>
        /// The tag value.
        /// </summary>
        [Required]
        public TagValueExtended Value { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagValueQueryResult"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public TagValueQueryResult(string tagId, string tagName, TagValueExtended value)
            : base(tagId, tagName) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }


        /// <summary>
        /// Creates a new <see cref="TagValueQueryResult"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueQueryResult Create(string tagId, string tagName, TagValueExtended value) {
            return new TagValueQueryResult(tagId, tagName, value);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueQueryResult"/> object.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagIdentifier"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueQueryResult Create(TagIdentifier tagIdentifier, TagValueExtended value) {
            return new TagValueQueryResult(tagIdentifier?.Id!, tagIdentifier?.Name!, value);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValueQueryResult"/>.
    /// </summary>
    internal class TagValueQueryResultConverter : AdapterJsonConverter<TagValueQueryResult> {


        /// <inheritdoc/>
        public override TagValueQueryResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string tagName = null!;
            TagValueExtended value = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueQueryResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueQueryResult.TagName), StringComparison.OrdinalIgnoreCase)) {
                    tagName = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueQueryResult.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<TagValueExtended>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueQueryResult.Create(tagId, tagName, value);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueQueryResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueQueryResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(TagValueQueryResult.TagName), value.TagName, options);
            WritePropertyValue(writer, nameof(TagValueQueryResult.Value), value.Value, options);
            writer.WriteEndObject();
        }

    }

}
