using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> for <see cref="System.Text.Json.JsonElement"/>.
    /// </summary>
    public class JsonElementConverter : JsonConverter<System.Text.Json.JsonElement> {

        /// <summary>
        /// System.Text.Json serializer options.
        /// </summary>
        private System.Text.Json.JsonSerializerOptions? _stjOptions;


        /// <summary>
        /// Creates a new <see cref="JsonElementConverter"/> instance.
        /// </summary>
        public JsonElementConverter(): this(null) { }


        /// <summary>
        /// Creates a new <see cref="JsonElementConverter"/> instance using the specified 
        /// <see cref="System.Text.Json.JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="options">
        ///   The <see cref="System.Text.Json.JsonSerializerOptions"/> to use.
        /// </param>
        public JsonElementConverter(System.Text.Json.JsonSerializerOptions? options) {
            _stjOptions = options;
        }


        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer) {
            if (value.ValueKind == System.Text.Json.JsonValueKind.Undefined) {
                writer.WriteNull();
                return;
            }
            writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value, _stjOptions));
        }


        /// <inheritdoc/>
        public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var token = serializer.Deserialize<JToken>(reader);
            return token == null 
                ? default 
                : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(token.ToString(), _stjOptions);
        }

    }

}
