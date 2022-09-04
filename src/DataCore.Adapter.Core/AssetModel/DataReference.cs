using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a data reference on an <see cref="AssetModelNode"/>. Note that the reference can 
    /// be to a tag on a different adapter.
    /// </summary>
    [JsonConverter(typeof(DataReferenceConverter))]
    public class DataReference {

        /// <summary>
        /// The adapter ID for the reference.
        /// </summary>
        public string AdapterId { get; }

        /// <summary>
        /// The tag name or ID for the reference.
        /// </summary>
        public string Tag { get; }


        /// <summary>
        /// Creates a new <see cref="DataReference"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the data reference.
        /// </param>
        /// <param name="tag">
        ///   The tag name or ID for the data reference.
        /// </param>
        public DataReference(string adapterId, string tag) {
            AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
        }

    }


    /// <summary>
    /// JSON converter for <see cref="DataReference"/>.
    /// </summary>
    internal class DataReferenceConverter : AdapterJsonConverter<DataReference> {

        /// <inheritdoc/>
        public override DataReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string adapterId = null!;
            string tag = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DataReference.AdapterId), StringComparison.OrdinalIgnoreCase)) {
                    adapterId = reader.GetString()!;
                }
                else if (string.Equals(propertyName, nameof(DataReference.Tag), StringComparison.OrdinalIgnoreCase)) {
                    tag = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new DataReference(adapterId, tag);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DataReference value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(DataReference.AdapterId), value.AdapterId, options);
            WritePropertyValue(writer, nameof(DataReference.Tag), value.Tag, options);

            writer.WriteEndObject();
        }
    }

}
