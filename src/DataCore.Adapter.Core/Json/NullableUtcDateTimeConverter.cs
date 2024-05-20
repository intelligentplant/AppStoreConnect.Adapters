using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> that ensures that nullable <see cref="DateTime"/> values are 
    /// always converted to UTC during serialization and deserializarion operations.
    /// </summary>
    public sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?> {

        /// <inheritdoc/>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            }

            if (!reader.TryGetDateTimeOffset(out var dateTimeOffset)) {
                throw new JsonException();
            }

            return dateTimeOffset.UtcDateTime;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToUniversalTime());
        }

    }

}
