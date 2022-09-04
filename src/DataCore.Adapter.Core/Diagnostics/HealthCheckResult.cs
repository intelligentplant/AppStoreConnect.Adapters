﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Represents the result of an adapter health check.
    /// </summary>
    [JsonConverter(typeof(HealthCheckResultConverter))]
    public struct HealthCheckResult {

        /// <summary>
        /// The display name of the health check result.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The status of the health check result.
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// The description of the health check that was performed.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The error that occurred when checking the status (if any).
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Additional data associated with the health check.
        /// </summary>
        public IDictionary<string, string> Data { get; }

        /// <summary>
        /// The inner results that contributed to the status of this result.
        /// </summary>
        public IEnumerable<HealthCheckResult>? InnerResults { get; }


        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/>.
        /// </summary>
        /// <param name="displayName">
        ///   The display name for the check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="status">
        ///   The health status for the check.
        /// </param>
        /// <param name="description">
        ///   A description of the check that was performed. Can be <see langword="null"/>.
        /// </param>
        /// <param name="error">
        ///   The error that occurred when checking the status (if any). Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <param name="data">
        ///   Additional data associated with the health check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="innerResults">
        ///   The inner results that contributed to the status of this result. Can be 
        ///   <see langword="null"/>.
        /// </param>
        public HealthCheckResult(string? displayName, HealthStatus status, string? description, string? error, IDictionary<string, string>? data, IEnumerable<HealthCheckResult>? innerResults) {
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? string.Empty
                : displayName!;
            Status = status;
            Description = description;
            Error = error;
            Data = new ReadOnlyDictionary<string, string>(data ?? new Dictionary<string, string>());
            InnerResults = innerResults?.ToArray();
        }


        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/> that is the composite of the specified 
        /// inner results.
        /// </summary>
        /// <param name="displayName">
        ///   The display name for the check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="innerResults">
        ///   The inner results. Can be <see langword="null"/>.
        /// </param>
        /// <param name="description">
        ///   A description of the check that was performed. Can be <see langword="null"/>.
        /// </param>
        /// <param name="data">
        ///   Additional data associated with the health check. Can be <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="HealthCheckResult"/>.
        /// </returns>
        public static HealthCheckResult Composite(
            string? displayName, 
            IEnumerable<HealthCheckResult>? innerResults, 
            string? description = null, 
            IDictionary<string, string>? data = null
        ) {
            if (innerResults == null) {
                return Healthy(displayName, description, data);
            }

            switch (GetAggregateHealthStatus(innerResults.Select(x => x.Status))) {
                case HealthStatus.Unhealthy:
                    return Unhealthy(displayName, description, null, data, innerResults);
                case HealthStatus.Degraded:
                    return Degraded(displayName, description, null, data, innerResults);
                default:
                    return Healthy(displayName, description, data, innerResults);
            }
        }


        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/> with healthy status.
        /// </summary>
        /// <param name="displayName">
        ///   The display name for the check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="description">
        ///   A description of the check that was performed. Can be <see langword="null"/>.
        /// </param>
        /// <param name="data">
        ///   Additional data associated with the health check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="innerResults">
        ///   The inner results that contributed to the status of this result. Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="HealthCheckResult"/>.
        /// </returns>
        public static HealthCheckResult Healthy(string? displayName, string? description = null, IDictionary<string, string>? data = null, IEnumerable<HealthCheckResult>? innerResults = null) {
            return new HealthCheckResult(displayName, HealthStatus.Healthy, description, null, data, innerResults);
        }


        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/> with degraded status.
        /// </summary>
        /// <param name="displayName">
        ///   The display name for the check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="description">
        ///   A description of the check that was performed. Can be <see langword="null"/>.
        /// </param>
        /// <param name="error">
        ///   The error that occurred when checking the status (if any). Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <param name="data">
        ///   Additional data associated with the health check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="innerResults">
        ///   The inner results that contributed to the status of this result. Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="HealthCheckResult"/>.
        /// </returns>
        public static HealthCheckResult Degraded(string? displayName, string? description = null, string? error = null, IDictionary<string, string>? data = null, IEnumerable<HealthCheckResult>? innerResults = null) {
            return new HealthCheckResult(displayName, HealthStatus.Degraded, description, error, data, innerResults);
        }


        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/> with unhealthy status.
        /// </summary>
        /// <param name="displayName">
        ///   The display name for the check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="description">
        ///   A description of the check that was performed. Can be <see langword="null"/>.
        /// </param>
        /// <param name="error">
        ///   The error that occurred when checking the status (if any). Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <param name="data">
        ///   Additional data associated with the health check. Can be <see langword="null"/>.
        /// </param>
        /// <param name="innerResults">
        ///   The inner results that contributed to the status of this result. Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="HealthCheckResult"/>.
        /// </returns>
        public static HealthCheckResult Unhealthy(string? displayName, string? description = null, string? error = null, IDictionary<string, string>? data = null, IEnumerable<HealthCheckResult>? innerResults = null) {
            return new HealthCheckResult(displayName, HealthStatus.Unhealthy, description, error, data, innerResults);
        }


        /// <summary>
        /// Gets the worst-case aggregate health status of the specified status collection.
        /// </summary>
        /// <param name="statuses">
        ///   The collection of health statuses to aggregate.
        /// </param>
        /// <returns>
        ///   The worst-case aggregate status of the collection.
        /// </returns>
        public static HealthStatus GetAggregateHealthStatus(IEnumerable<HealthStatus> statuses) {
            if (statuses == null) {
                throw new ArgumentNullException(nameof(statuses));
            }

            return statuses.Aggregate(HealthStatus.Healthy, (previous, current) => current < previous ? current : previous);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="HealthCheckResultConverter"/>.
    /// </summary>
    internal class HealthCheckResultConverter : AdapterJsonConverter<HealthCheckResult> {

        /// <inheritdoc/>
        public override HealthCheckResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string? displayName = default;
            HealthStatus status = default;
            string? description = default;
            string? error = default;
            IDictionary<string, string>? data = default;
            IEnumerable<HealthCheckResult>? innerResults = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(HealthCheckResult.DisplayName), StringComparison.OrdinalIgnoreCase)) {
                    displayName = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<HealthStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(HealthCheckResult.Error), StringComparison.OrdinalIgnoreCase)) {
                    error = JsonSerializer.Deserialize<string>(ref reader, options);
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

            return new HealthCheckResult(displayName, status, description, error, data, innerResults);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, HealthCheckResult value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(HealthCheckResult.DisplayName), value.DisplayName, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Status), value.Status, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Description), value.Description, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Error), value.Error, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.Data), value.Data, options);
            WritePropertyValue(writer, nameof(HealthCheckResult.InnerResults), value.InnerResults, options);

            writer.WriteEndObject();
        }
    }

}
