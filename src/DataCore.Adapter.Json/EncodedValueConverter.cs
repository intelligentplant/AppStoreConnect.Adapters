using System;
using System.Collections.Generic;
using System.Text.Json;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="EncodedValue"/>.
    /// </summary>
    public class EncodedValueConverter : AdapterJsonConverter<EncodedValue> {

        /// <inheritdoc/>
        public override EncodedValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            Uri uri = null;
            string contentType = null;
            IEnumerable<byte> value = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(EncodedValue.TypeUri), StringComparison.OrdinalIgnoreCase)) {
                    uri = JsonSerializer.Deserialize<Uri>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EncodedValue.ContentType), StringComparison.OrdinalIgnoreCase)) {
                    contentType = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EncodedValue.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<IEnumerable<byte>>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new EncodedValue(uri, contentType, value);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, EncodedValue value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(EncodedValue.TypeUri), value.TypeUri, options);
            WritePropertyValue(writer, nameof(EncodedValue.ContentType), value.ContentType, options);
            WritePropertyValue(writer, nameof(EncodedValue.Value), value.Value, options);

            writer.WriteEndObject();
        }

    }
}
