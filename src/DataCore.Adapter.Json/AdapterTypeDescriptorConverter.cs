using System;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="AdapterTypeDescriptor"/>.
    /// </summary>
    public class AdapterTypeDescriptorConverter : AdapterJsonConverter<AdapterTypeDescriptor> {

        /// <inheritdoc/>
        public override AdapterTypeDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            Uri uri = null!;
            string? name = null!;
            string? description = null!;
            string? version = null!;
            VendorInfo? vendor = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Uri), StringComparison.OrdinalIgnoreCase)) {
                    uri = JsonSerializer.Deserialize<Uri>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Version), StringComparison.OrdinalIgnoreCase)) {
                    version = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Vendor), StringComparison.OrdinalIgnoreCase)) {
                    vendor = JsonSerializer.Deserialize<VendorInfo>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new AdapterTypeDescriptor(uri, name, description, version, vendor);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterTypeDescriptor value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Uri), value.Uri, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Version), value.Version, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Vendor), value.Vendor, options);

            writer.WriteEndObject();
        }

    }
}
