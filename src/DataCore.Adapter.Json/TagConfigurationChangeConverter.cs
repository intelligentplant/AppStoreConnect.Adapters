using System;
using System.Collections.Generic;
using System.Text.Json;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagConfigurationChange"/>.
    /// </summary>
    public class TagConfigurationChangeConverter : AdapterJsonConverter<TagConfigurationChange> {


        /// <inheritdoc/>
        public override TagConfigurationChange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            TagIdentifier tag = null!;
            ConfigurationChangeType changeType = default;
            IEnumerable<AdapterProperty> properties = default!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagConfigurationChange.Tag), StringComparison.OrdinalIgnoreCase)) {
                    tag = JsonSerializer.Deserialize<TagIdentifier>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagConfigurationChange.ChangeType), StringComparison.OrdinalIgnoreCase)) {
                    changeType = JsonSerializer.Deserialize<ConfigurationChangeType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagConfigurationChange.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<IEnumerable<AdapterProperty>>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new TagConfigurationChange(tag, changeType, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagConfigurationChange value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagConfigurationChange.Tag), value.Tag, options);
            WritePropertyValue(writer, nameof(TagConfigurationChange.ChangeType), value.ChangeType, options);
            WritePropertyValue(writer, nameof(TagConfigurationChange.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
