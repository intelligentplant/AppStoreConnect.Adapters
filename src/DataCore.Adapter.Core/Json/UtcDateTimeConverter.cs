using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> that ensures that <see cref="DateTime"/> values are always 
    /// converted to UTC during serialization and deserializarion operations.
    /// </summary>
    public sealed class UtcDateTimeConverter : JsonConverter<DateTime> {

        /// <inheritdoc/>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (!reader.TryGetDateTimeOffset(out var dateTimeOffset)) {
                throw new JsonException();
            }

            return dateTimeOffset.UtcDateTime;
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToUniversalTime());
        }

    }

}
