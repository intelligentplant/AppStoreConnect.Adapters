using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DataCore.Adapter.Extensions;

using Newtonsoft.Json;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// <see cref="IValueEncoder"/> that encodes to/from JSON using <see cref="JsonConvert"/>.
    /// </summary>
    public class NewtonsoftJsonValueEncoder : ValueEncoder {

        /// <summary>
        /// Serializer settings.
        /// </summary>
        private readonly JsonSerializerSettings _options;


        /// <summary>
        /// Creates a new <see cref="NewtonsoftJsonValueEncoder"/> object.
        /// </summary>
        /// <param name="options">
        ///   The JSON serializer options to use.
        /// </param>
        public NewtonsoftJsonValueEncoder(JsonSerializerSettings options = null) : base("application/json") {
            _options = options;
        }


        /// <inheritdoc/>
        protected override IEnumerable<byte> Encode<T>(T value) {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, Formatting.None, _options));
        }


        /// <inheritdoc/>
        protected override T Decode<T>(IEnumerable<byte> value) {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(value is byte[] bytes ? bytes : value.ToArray()), _options);
        }

    }
}
