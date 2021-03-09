using System;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="ExtensionObject"/>.
    /// </summary>
    public class ExtensionObjectConverter : AdapterJsonConverter<ExtensionObject> {


        /// <inheritdoc/>
        public override ExtensionObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            Uri typeId = null!;
            string encoding = null!;
            byte[] encodedBody = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(ExtensionObject.TypeId), StringComparison.OrdinalIgnoreCase)) {
                    typeId = JsonSerializer.Deserialize<Uri>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ExtensionObject.Encoding), StringComparison.OrdinalIgnoreCase)) {
                    encoding = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(ExtensionObject.EncodedBody), StringComparison.OrdinalIgnoreCase)) {
                    // Body is encoded as a base64web string.
                    var base64webBody = JsonSerializer.Deserialize<string>(ref reader, options);
                    if (base64webBody == null) {
                        encodedBody = Array.Empty<byte>();
                    }
                    else {
                        encodedBody = Base64WebDecode(base64webBody);
                    }
                }
                else {
                    reader.Skip();
                }
            }

            return new ExtensionObject(typeId, encoding, encodedBody);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ExtensionObject value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(ExtensionObject.TypeId), value.TypeId, options);
            WritePropertyValue(writer, nameof(ExtensionObject.Encoding), value.Encoding, options);
            WritePropertyValue(writer, nameof(ExtensionObject.EncodedBody), Base64WebEncode(value.EncodedBody), options);
            writer.WriteEndObject();
        }


        /// <summary>
        /// Converts the specified bytes to a base64web string.
        /// </summary>
        /// <param name="bytes">
        ///   The bytes.
        /// </param>
        /// <returns>
        ///   The encoded base64web string.
        /// </returns>
        private static string Base64WebEncode(byte[] bytes) {
            return Convert.ToBase64String(bytes).Replace('/', '_').Replace('+', '-').TrimEnd('=');
        }


        /// <summary>
        /// Converts the specified base64web string to a byte array.
        /// </summary>
        /// <param name="bytes">
        ///   The base64web-encoded byte string.
        /// </param>
        /// <returns>
        ///   The byte array.
        /// </returns>
        private static byte[] Base64WebDecode(string bytes) {
            var base64 = bytes
                .Replace('-', '+')
                .Replace('_', '/');

            switch (base64.Length % 4) {
                case 0:
                    // No padding required
                    break;
                case 2:
                    // Pad with '=='
                    base64 = base64 + "==";
                    break;
                case 3:
                    // Pad with '='
                    base64 = base64 + "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }


        /// <summary>
        /// Creates a new <see cref="ExtensionObject"/> with the specified type ID and value that 
        /// encodes the value using JSON.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="typeId">
        ///   The type ID.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="options">
        ///   Options to control the conversion behaviour.
        /// </param>
        /// <returns>
        ///   A new <see cref="ExtensionObject"/> with an <see cref="ExtensionObject.Encoding"/> 
        ///   set to <c>application/json</c> and a JSON-encoded <see cref="ExtensionObject.EncodedBody"/>.
        /// </returns>
        public static ExtensionObject CreateExtensionObject<T>(Uri typeId, T value, JsonSerializerOptions? options = null) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }

            if (value == null) {
                return new ExtensionObject(typeId, "application/json", Array.Empty<byte>());
            }

            return new ExtensionObject(typeId, "application/json", JsonSerializer.SerializeToUtf8Bytes(value, options));
        }

    }
}
