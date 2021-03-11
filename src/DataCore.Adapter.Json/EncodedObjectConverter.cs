using System;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="EncodedObject"/>.
    /// </summary>
    public class EncodedObjectConverter : AdapterJsonConverter<EncodedObject> {

        /// <inheritdoc/>
        public override EncodedObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            Uri typeId = null!;
            string encoding = null!;
            string encodedBody = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(EncodedObject.TypeId), StringComparison.OrdinalIgnoreCase)) {
                    typeId = JsonSerializer.Deserialize<Uri>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(EncodedObject.Encoding), StringComparison.OrdinalIgnoreCase)) {
                    encoding = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(EncodedObject.EncodedBody), StringComparison.OrdinalIgnoreCase)) {
                    // Body is encoded as a base64 string.
                    encodedBody = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new EncodedObject(typeId, encoding, encodedBody);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, EncodedObject value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(EncodedObject.TypeId), value.TypeId, options);
            WritePropertyValue(writer, nameof(EncodedObject.Encoding), value.Encoding, options);
            WritePropertyValue(writer, nameof(EncodedObject.EncodedBody), value.EncodedBody, options);
            writer.WriteEndObject();
        }

    }
}
