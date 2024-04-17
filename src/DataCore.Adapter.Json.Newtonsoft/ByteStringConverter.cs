using System;

using DataCore.Adapter.Common;

using Newtonsoft.Json;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// JSON converter for <see cref="ByteString"/>.
    /// </summary>
    public class ByteStringConverter : JsonConverter<ByteString> {

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, ByteString value, JsonSerializer serializer) {
            if (value.IsEmpty) {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.Bytes.ToArray());
        }


        /// <inheritdoc/>
        public override ByteString ReadJson(JsonReader reader, Type objectType, ByteString existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null) {
                return ByteString.Empty;
            }

            if (reader.TokenType != JsonToken.Bytes) {
                throw new JsonException();
            }

            return new ByteString(reader.ReadAsBytes()!);
        }

    }
}
