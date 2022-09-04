using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a real-time or historical value on a tag.
    /// </summary>
    [JsonConverter(typeof(TagValueExtendedConverter))]
    public sealed class TagValueExtended : TagValue {

        /// <summary>
        /// Notes associated with the value.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// An error message associated with the value.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Additional value properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagValueExtended"/> object.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <param name="value">
        ///   The values for the sample.
        /// </param>
        /// <param name="status">
        ///   The quality status for the value.
        /// </param>
        /// <param name="units">
        ///   The value units.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the value.
        /// </param>
        /// <param name="error">
        ///   An error message to associate with the value.
        /// </param>
        /// <param name="properties">
        ///   Custom properties associated with the value.
        /// </param>
        public TagValueExtended(
            DateTime utcSampleTime, 
            Variant value,
            TagValueStatus status, 
            string? units, 
            string? notes, 
            string? error, 
            IEnumerable<AdapterProperty>? properties
        ) : base(utcSampleTime, value, status, units) {
            Notes = notes;
            Error = error;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueExtended"/> object.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <param name="additionalValues">
        ///   Additional tag values e.g. if <paramref name="value"/> is the value of a digital 
        ///   state, the name of the state can be specified by passing in an additional value.
        /// </param>
        /// <param name="status">
        ///   The quality status for the value.
        /// </param>
        /// <param name="units">
        ///   The value units.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the value.
        /// </param>
        /// <param name="error">
        ///   An error message to associate with the value.
        /// </param>
        /// <param name="properties">
        ///   Custom properties associated with the value.
        /// </param>
        [Obsolete("Use constructor directly", true)]
        public static TagValueExtended Create(DateTime utcSampleTime, Variant value, IEnumerable<Variant>? additionalValues, TagValueStatus status, string? units, string? notes, string? error, IEnumerable<AdapterProperty>? properties) {
            return new TagValueExtended(utcSampleTime, value, status, units, notes, error, properties);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValue"/>.
    /// </summary>
    internal class TagValueExtendedConverter : AdapterJsonConverter<TagValueExtended> {

        /// <inheritdoc/>
        public override TagValueExtended Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            DateTime utcSampleTime = default;
            Variant value = Variant.Null;
            TagValueStatus status = TagValueStatus.Uncertain;
            string units = null!;
            string notes = null!;
            string error = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueExtended.UtcSampleTime), StringComparison.OrdinalIgnoreCase)) {
                    utcSampleTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<TagValueStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Notes), StringComparison.OrdinalIgnoreCase)) {
                    notes = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Error), StringComparison.OrdinalIgnoreCase)) {
                    error = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new TagValueExtended(utcSampleTime, value, status, units, notes, error, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueExtended value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueExtended.UtcSampleTime), value.UtcSampleTime, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Status), value.Status, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Units), value.Units, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Error), value.Error, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }

}
