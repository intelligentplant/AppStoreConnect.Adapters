using System;
using System.Text.Json;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="AssetModelNodeMeasurement"/>.
    /// </summary>
    public class AssetModelNodeMeasurementConverter : AdapterJsonConverter<AssetModelNodeMeasurement> {


        /// <inheritdoc/>
        public override AssetModelNodeMeasurement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null;
            string adapterId = null;
            TagSummary tag = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AssetModelNodeMeasurement.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNodeMeasurement.AdapterId), StringComparison.OrdinalIgnoreCase)) {
                    adapterId = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNodeMeasurement.Tag), StringComparison.OrdinalIgnoreCase)) {
                    tag = JsonSerializer.Deserialize<TagSummary>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return AssetModelNodeMeasurement.Create(name, adapterId, tag);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AssetModelNodeMeasurement value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(AssetModelNodeMeasurement.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AssetModelNodeMeasurement.AdapterId), value.AdapterId, options);
            WritePropertyValue(writer, nameof(AssetModelNodeMeasurement.Tag), value.Tag, options);
            writer.WriteEndObject();
        }

    }
}
