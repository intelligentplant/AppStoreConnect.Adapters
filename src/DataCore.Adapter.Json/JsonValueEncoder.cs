using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// <see cref="IValueEncoder"/> that encodes to/from JSON using <see cref="JsonSerializer"/>.
    /// </summary>
    public class JsonValueEncoder : ValueEncoder {

        /// <summary>
        /// Serializer options.
        /// </summary>
        private readonly JsonSerializerOptions _options;


        /// <summary>
        /// Creates a new <see cref="JsonValueEncoder"/> object.
        /// </summary>
        /// <param name="options">
        ///   The JSON serializer options to use.
        /// </param>
        public JsonValueEncoder(JsonSerializerOptions options = null) : base("application/json") {
            _options = options;
        }


        /// <inheritdoc/>
        protected override IEnumerable<byte> Encode<T>(T value) {
            return JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }


        /// <inheritdoc/>
        protected override T Decode<T>(IEnumerable<byte> value) {
            return JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(value is byte[] bytes ? bytes : value.ToArray()), _options);
        }

    }
}
