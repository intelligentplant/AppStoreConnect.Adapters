using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Describes a configuration change for a tag.
    /// </summary>
    [JsonConverter(typeof(ConfigurationChangeConverter))]
    public class ConfigurationChange {

        /// <summary>
        /// The type of the item.
        /// </summary>
        public string ItemType { get; }

        /// <summary>
        /// The ID of the item that was modified.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// The display name of the item that was modified.
        /// </summary>
        public string ItemName { get; }

        /// <summary>
        /// The change type.
        /// </summary>
        public ConfigurationChangeType ChangeType { get; }

        /// <summary>
        /// Additional properties associated with the change.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="ConfigurationChange"/> object.
        /// </summary>
        /// <param name="itemType">
        ///   The type of the item that was modified. Common values are defined in <see cref="ConfigurationChangeItemTypes"/>.
        /// </param>
        /// <param name="itemId">
        ///   The ID of the item that was modified.
        /// </param>
        /// <param name="itemName">
        ///   The display name of the item that was modified.
        /// </param>
        /// <param name="changeType">
        ///   The change type.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the change.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="itemId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="itemName"/> is <see langword="null"/> or white space.
        /// </exception>
        public ConfigurationChange(string itemType, string itemId, string itemName, ConfigurationChangeType changeType, IEnumerable<AdapterProperty>? properties) {
            if (string.IsNullOrWhiteSpace(itemType)) {
                throw new ArgumentException(SharedResources.Error_TypeIsRequired, nameof(itemType));
            }
            if (string.IsNullOrWhiteSpace(itemId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(itemId));
            }
            if (string.IsNullOrWhiteSpace(itemName)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(itemName));
            }
            ItemType = itemType;
            ItemId = itemId;
            ItemName = itemName;
            ChangeType = changeType;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }

    }


    /// <summary>
    /// JSON converter for <see cref="ConfigurationChange"/>.
    /// </summary>
    internal class ConfigurationChangeConverter : AdapterJsonConverter<ConfigurationChange> {

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
                    itemType = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.ItemId), StringComparison.OrdinalIgnoreCase)) {
                    itemId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.ItemName), StringComparison.OrdinalIgnoreCase)) {
                    itemName = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.ChangeType), StringComparison.OrdinalIgnoreCase)) {
                    changeType = JsonSerializer.Deserialize<ConfigurationChangeType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ConfigurationChange.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<IEnumerable<AdapterProperty>>(ref reader, options)!;
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
