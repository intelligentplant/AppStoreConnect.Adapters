using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TimeSpan"/>. There is (astonishingly) no native support for 
    /// <see cref="TimeSpan"/> in System.Text.Json: https://github.com/dotnet/runtime/issues/29932
    /// </summary>
    public sealed class TimeSpanConverter : JsonConverter<TimeSpan> {

        /// <inheritdoc/>
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return TimeSpan.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
            writer?.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
        }

    }
}
