using System;
using System.Collections.Generic;
using System.Text.Json;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="HostInfo"/>.
    /// </summary>
    public class HostInfoConverter : AdapterJsonConverter<HostInfo> {

        /// <inheritdoc/>
        public override HostInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null;
            string description = null;
            string version = null;
            VendorInfo vendor = null;
            IEnumerable<AdapterProperty> properties = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(HostInfo.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Version), StringComparison.OrdinalIgnoreCase)) {
                    version = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Vendor), StringComparison.OrdinalIgnoreCase)) {
                    vendor = JsonSerializer.Deserialize<VendorInfo>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<IEnumerable<AdapterProperty>>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return HostInfo.Create(name, description, version, vendor, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, HostInfo value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteString(ConvertPropertyName(nameof(HostInfo.Name), options), value.Name);
            writer.WriteString(ConvertPropertyName(nameof(HostInfo.Description), options), value.Description);
            writer.WriteString(ConvertPropertyName(nameof(HostInfo.Version), options), value.Version);

            writer.WritePropertyName(ConvertPropertyName(nameof(HostInfo.Vendor), options));
            JsonSerializer.Serialize(writer, value.Vendor, options);

            WritePropertyValue(writer, nameof(HostInfo.Properties), value.Properties, options);

            writer.WriteEndObject();
        }
    }
}
