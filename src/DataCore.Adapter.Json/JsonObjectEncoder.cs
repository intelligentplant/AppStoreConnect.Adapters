using System;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// <see cref="IObjectEncoder"/> that encodes to/decodes from JSON.
    /// </summary>
    public class JsonObjectEncoder : ObjectEncoder {

        /// <inheritdoc/>
        public override string EncodingType => "json";

        /// <summary>
        /// The options to use in conversion to/from JSON.
        /// </summary>
        private readonly JsonSerializerOptions? _options;


        /// <summary>
        /// Creates a new <see cref="JsonObjectEncoder"/> object.
        /// </summary>
        /// <param name="options">
        ///   The options to use in conversion to/from JSON.
        /// </param>
        public JsonObjectEncoder(JsonSerializerOptions? options = null) {
            _options = options;
        }


        /// <inheritdoc/>
        protected override byte[] Encode(object? value, Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            return JsonSerializer.SerializeToUtf8Bytes(value, type, _options);
        }


        /// <inheritdoc/>
        protected override object? Decode(byte[]? encodedData, Type type) {
            if (encodedData == null) {
                throw new ArgumentNullException(nameof(encodedData));
            }
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            return JsonSerializer.Deserialize(encodedData, type, _options);
        }

    }
}
