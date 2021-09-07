using System;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="StatusCode"/>.
    /// </summary>
    public class StatusCodeConverter : AdapterJsonConverter<StatusCode> {

        /// <inheritdoc/>
        public override StatusCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return reader.GetUInt32();
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, StatusCode value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.Value);
        }

    }
}
