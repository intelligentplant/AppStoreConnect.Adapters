using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a result for an annotations query on a tag.
    /// </summary>
    [JsonConverter(typeof(TagValueAnnotationQueryResultConverter))]
    public class TagValueAnnotationQueryResult : TagDataContainer {

        /// <summary>
        /// The annotation.
        /// </summary>
        [Required]
        public TagValueAnnotationExtended Annotation { get; }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationQueryResult"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotation"/> is <see langword="null"/>.
        /// </exception>
        public TagValueAnnotationQueryResult(string tagId, string tagName, TagValueAnnotationExtended annotation) 
            : base(tagId, tagName) {
            Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationQueryResult"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotation"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueAnnotationQueryResult Create(string tagId, string tagName, TagValueAnnotationExtended annotation) {
            return new TagValueAnnotationQueryResult(tagId, tagName, annotation);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationQueryResult"/> object.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagIdentifier"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotation"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueAnnotationQueryResult Create(TagIdentifier tagIdentifier, TagValueAnnotationExtended annotation) {
            return new TagValueAnnotationQueryResult(tagIdentifier?.Id!, tagIdentifier?.Name!, annotation);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValueAnnotationQueryResult"/>.
    /// </summary>
    internal class TagValueAnnotationQueryResultConverter : AdapterJsonConverter<TagValueAnnotationQueryResult> {

        /// <inheritdoc/>
        public override TagValueAnnotationQueryResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string tagName = null!;
            TagValueAnnotationExtended annotation = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueAnnotationQueryResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationQueryResult.TagName), StringComparison.OrdinalIgnoreCase)) {
                    tagName = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationQueryResult.Annotation), StringComparison.OrdinalIgnoreCase)) {
                    annotation = JsonSerializer.Deserialize<TagValueAnnotationExtended>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueAnnotationQueryResult.Create(tagId, tagName, annotation);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueAnnotationQueryResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueAnnotationQueryResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationQueryResult.TagName), value.TagName, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationQueryResult.Annotation), value.Annotation, options);
            writer.WriteEndObject();
        }

    }

}
