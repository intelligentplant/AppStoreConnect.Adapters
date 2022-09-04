using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {
    /// <summary>
    /// Describes a value returned by a tag value query for processed data.
    /// </summary>
    [JsonConverter(typeof(ProcessedTagValueQueryResultConverter))]
    public class ProcessedTagValueQueryResult : TagValueQueryResult {

        /// <summary>
        /// The data function used to aggregate the tag value.
        /// </summary>
        [Required]
        public string DataFunction { get; set; }


        /// <summary>
        /// Creates a new <see cref="ProcessedTagValueQueryResult"/> object.
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
        /// <param name="dataFunction">
        ///   The data function used to aggregate the tag value.
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
        public ProcessedTagValueQueryResult(
            string tagId,
            string tagName,
            TagValueExtended value,
            string dataFunction
        ) : base(tagId, tagName, value) {
            DataFunction = dataFunction ?? throw new ArgumentNullException(nameof(dataFunction));
        }


        /// <summary>
        /// Creates a new <see cref="ProcessedTagValueQueryResult"/> object.
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
        /// <param name="dataFunction">
        ///   The data function used to aggregate the tag value.
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
        public static ProcessedTagValueQueryResult Create(string tagId, string tagName, TagValueExtended value, string dataFunction) {
            return new ProcessedTagValueQueryResult(tagId, tagName, value, dataFunction);
        }


        /// <summary>
        /// Creates a new <see cref="ProcessedTagValueQueryResult"/> object.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <param name="dataFunction">
        ///   The data function used to aggregate the tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagIdentifier"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static ProcessedTagValueQueryResult Create(TagIdentifier tagIdentifier, TagValueExtended value, string dataFunction) {
            return new ProcessedTagValueQueryResult(tagIdentifier?.Id!, tagIdentifier?.Name!, value, dataFunction);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValueQueryResult"/>.
    /// </summary>
    internal class ProcessedTagValueQueryResultConverter : AdapterJsonConverter<ProcessedTagValueQueryResult> {

        /// <inheritdoc/>
        public override ProcessedTagValueQueryResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string tagName = null!;
            TagValueExtended value = null!;
            string dataFunction = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.TagName), StringComparison.OrdinalIgnoreCase)) {
                    tagName = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<TagValueExtended>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.DataFunction), StringComparison.OrdinalIgnoreCase)) {
                    dataFunction = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return ProcessedTagValueQueryResult.Create(tagId, tagName, value, dataFunction);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ProcessedTagValueQueryResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.TagName), value.TagName, options);
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.Value), value.Value, options);
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.DataFunction), value.DataFunction, options);
            writer.WriteEndObject();
        }

    }

}
