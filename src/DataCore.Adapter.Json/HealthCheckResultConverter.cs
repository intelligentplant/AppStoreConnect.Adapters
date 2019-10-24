using System;
using System.Collections.Generic;
using System.Text.Json;
using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="HostInfo"/>.
    /// </summary>
    public class HealthCheckResultConverter : AdapterJsonConverter<HealthCheckResult> {

        /// <inheritdoc/>
        public override HealthCheckResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            HealthStatus status = default;
            string description = default;
            string error = default;
            IDictionary<string, string> data = default;
            IEnumerable<HealthCheckResult> innerResults = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(HealthCheckResult.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<HealthStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.Error), StringComparison.OrdinalIgnoreCase)) {
                    error = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.Data), StringComparison.OrdinalIgnoreCase)) {
                    data = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.InnerResults), StringComparison.OrdinalIgnoreCase)) {
                    innerResults = JsonSerializer.Deserialize<IEnumerable<HealthCheckResult>>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new HealthCheckResult(status, description, error, data, innerResults);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, HealthCheckResult value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(HealthCheckResult.Status), value.Status, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Description), value.Description, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Error), value.Error, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Data), value.Data, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.InnerResults), value.InnerResults, options);

            writer.WriteEndObject();
        }
    }
}
