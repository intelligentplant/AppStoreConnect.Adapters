using System;

using DataCore.Adapter.Common;

using Newtonsoft.Json;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> for <see cref="StatusCode"/>.
    /// </summary>
    public class StatusCodeConverter : JsonConverter<StatusCode> {

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, StatusCode value, JsonSerializer serializer) {
            writer.WriteValue(value.Value);
        }

        /// <inheritdoc/>
        public override StatusCode ReadJson(JsonReader reader, Type objectType, StatusCode existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.Value == null) {
                return default;
            }

            return Convert.ToUInt32(reader.Value);
        }

    }
}
