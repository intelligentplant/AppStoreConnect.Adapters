using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the vendor for the hosting application.
    /// </summary>
    [JsonConverter(typeof(VendorInfoConverter))]
    public class VendorInfo {

        /// <summary>
        /// The vendor name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The vendor URL.
        /// </summary>
        public string? Url { get; }



        /// <summary>
        /// Creates a new <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="url">
        ///   The vendor URL.
        /// </param>
        public VendorInfo(string? name, string? url) {
            Name = name?.Trim();
            Url = url;
        }


        /// <summary>
        /// Creates a new <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="url">
        ///   The vendor URL.
        /// </param>
        public static VendorInfo Create(string? name, string? url) {
            return new VendorInfo(name, url);
        }


        /// <summary>
        /// Creates a copy of a <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="vendorInfo">
        ///   The object to copy.
        /// </param>
        /// <returns>
        ///   A new <see cref="VendorInfo"/> object, with properties copied from the existing instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="vendorInfo"/> is <see langword="null"/>.
        /// </exception>
        public static VendorInfo FromExisting(VendorInfo vendorInfo) {
            if (vendorInfo == null) {
                throw new ArgumentNullException(nameof(vendorInfo));
            }

            return Create(vendorInfo.Name, vendorInfo.Url);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="VendorInfo"/>.
    /// </summary>
    internal class VendorInfoConverter : AdapterJsonConverter<VendorInfo> {


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
