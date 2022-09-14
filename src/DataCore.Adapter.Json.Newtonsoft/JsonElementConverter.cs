using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> for <see cref="System.Text.Json.JsonElement"/>.
    /// </summary>
    public class JsonElementConverter : JsonConverter<System.Text.Json.JsonElement> {

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer) {
            writer.WriteRawValue(value.ToString());
        }


        /// <inheritdoc/>
        public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var token = serializer.Deserialize<JToken>(reader);
            return token == null 
                ? default 
                : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(token.ToString());
        }

    }


    /// <summary>
    /// <see cref="JsonConverter{T}"/> for a nullable <see cref="System.Text.Json.JsonElement"/>.
    /// </summary>
    public class NullableJsonElementConverter : JsonConverter<System.Text.Json.JsonElement?> {

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement? value, JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }

            writer.WriteRawValue(value.Value.ToString());
        }


        /// <inheritdoc/>
        public override System.Text.Json.JsonElement? ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var token = serializer.Deserialize<JToken>(reader);
            return token == null
                ? default
                : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(token.ToString());
        }

    }

}
