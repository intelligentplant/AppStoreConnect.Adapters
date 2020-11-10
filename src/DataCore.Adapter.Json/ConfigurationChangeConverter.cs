using System;
using System.Collections.Generic;
using System.Text.Json;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="ConfigurationChange"/>.
    /// </summary>
    public class ConfigurationChangeConverter : AdapterJsonConverter<ConfigurationChange> {

        /// <inheritdoc/>
        public override ConfigurationChange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string itemType = default!;
            string itemId = default!;
            string itemName = default!;
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

                if (string.Equals(propertyName, nameof(ConfigurationChange.ItemType), StringComparison.OrdinalIgnoreCase)) {
                    itemType = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.ItemId), StringComparison.OrdinalIgnoreCase)) {
                    itemId = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.ItemName), StringComparison.OrdinalIgnoreCase)) {
                    itemName = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.ChangeType), StringComparison.OrdinalIgnoreCase)) {
                    changeType = JsonSerializer.Deserialize<ConfigurationChangeType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<IEnumerable<AdapterProperty>>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new ConfigurationChange(itemType, itemId, itemName, changeType, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ConfigurationChange value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(ConfigurationChange.ItemType), value.ItemType, options);
            WritePropertyValue(writer, nameof(ConfigurationChange.ItemId), value.ItemId, options);
            WritePropertyValue(writer, nameof(ConfigurationChange.ItemName), value.ItemName, options);
            WritePropertyValue(writer, nameof(ConfigurationChange.ChangeType), value.ChangeType, options);
            WritePropertyValue(writer, nameof(ConfigurationChange.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
