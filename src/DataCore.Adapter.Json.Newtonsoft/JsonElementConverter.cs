using System;
using System.Linq;
using System.Text.Json;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> for <see cref="JsonElement"/> from the <c>System.Text.Json</c> 
    /// serializer.
    /// </summary>
    public class JsonElementConverter : JsonConverter<JsonElement> { 

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, JsonElement value, Newtonsoft.Json.JsonSerializer serializer) {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            writer.WriteRawValue(json);
        }


        /// <inheritdoc/>
        public override JsonElement ReadJson(JsonReader reader, Type objectType, JsonElement existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer) {
            var token = serializer.Deserialize<JToken>(reader);
            if (token == null) {
                return default;
            }

            return JsonDocument.Parse(token.ToString(serializer.Formatting, serializer.Converters.ToArray())).RootElement;
        }

    }


    /// <summary>
    /// <see cref="JsonConverter{T}"/> for a nullable <see cref="JsonElement"/>.
    /// </summary>
    public class NullableJsonElementConverter : JsonConverter<JsonElement?> {

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, JsonElement? value, Newtonsoft.Json.JsonSerializer serializer) {
            if (value == null) {
                writer.WriteNull();
                return;
            }
            var json = System.Text.Json.JsonSerializer.Serialize(value.Value);
            writer.WriteRawValue(json);
        }


        /// <inheritdoc/>
        public override JsonElement? ReadJson(JsonReader reader, Type objectType, JsonElement? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer) {
            var token = serializer.Deserialize<JToken>(reader);
            if (token == null) {
                return default;
            }

            return JsonDocument.Parse(token.ToString(serializer.Formatting, serializer.Converters.ToArray())).RootElement;
        }

    }

}
