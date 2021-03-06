﻿using System;
using System.Text.Json;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="VendorInfo"/>.
    /// </summary>
    public class VendorInfoConverter : AdapterJsonConverter<VendorInfo> {


        /// <inheritdoc/>
        public override VendorInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null!;
            string url = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(VendorInfo.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(VendorInfo.Url), StringComparison.OrdinalIgnoreCase)) {
                    url = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return VendorInfo.Create(name, url);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, VendorInfo value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(VendorInfo.Name), value.Name, options);
            WritePropertyValue(writer, nameof(VendorInfo.Url), value.Url, options);
            writer.WriteEndObject();
        }

    }
}
