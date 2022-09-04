using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the base set of properties for a tag value sample.
    /// </summary>
    [JsonConverter(typeof(TagValueConverter))]
    public class TagValue : IFormattable {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; }

        /// <summary>
        /// The value for the sample.
        /// </summary>
        public Variant Value { get; }

        /// <summary>
        /// The quality status for the value.
        /// </summary>
        public TagValueStatus Status { get; }

        /// <summary>
        /// The value units.
        /// </summary>
        public string? Units { get; }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> object.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <param name="value">
        ///   The value for the sample.
        /// </param>
        /// <param name="status">
        ///   The quality status for the value.
        /// </param>
        /// <param name="units">
        ///   The value units.
        /// </param>
        public TagValue(DateTime utcSampleTime, Variant value, TagValueStatus status, string? units) {
            UtcSampleTime = utcSampleTime;
            Value = value;
            Status = status;
            Units = units;
        }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> object.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <param name="status">
        ///   The quality status for the value.
        /// </param>
        /// <param name="units">
        ///   The value units.
        /// </param>
        [Obsolete("Use constructor directly.", true)]
        public static TagValue Create(DateTime utcSampleTime, Variant value, TagValueStatus status, string? units) {
            return new TagValue(utcSampleTime, value, status, units);
        }


        /// <inheritdoc/>
        public override string ToString() {
            return ToString(null, null);
        }


        /// <summary>
        /// Formats the value of the current instance using the specified format.
        /// </summary>
        /// <param name="format">
        ///   The format to use.
        /// </param>
        /// <returns>
        ///   The formatted value.
        /// </returns>
        public string ToString(string? format) {
            return ToString(format, null);
        }


        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider) {
            var formattedValue = Value.ToString(format, formatProvider);
            var formattedTimestamp = UtcSampleTime.ToString(Variant.DefaultDateTimeFormat, formatProvider);
            var formattedStatus = Status == TagValueStatus.Good
                ? SharedResources.TagValueStatus_Good
                : Status == TagValueStatus.Bad
                    ? SharedResources.TagValueStatus_Bad
                    : SharedResources.TagValueStatus_Uncertain;

            if (string.IsNullOrWhiteSpace(Units)) {
                return string.Concat(formattedValue, " @ ", formattedTimestamp, " [", formattedStatus, "]");
            }

            return string.Concat(formattedValue, " ", Units, " @ ", formattedTimestamp, " [", formattedStatus, "]");
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValue"/>.
    /// </summary>
    internal class TagValueConverter : AdapterJsonConverter<TagValue> {

        /// <inheritdoc/>
        public override TagValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            DateTime utcSampleTime = default;
            Variant value = Variant.Null;
            TagValueStatus status = TagValueStatus.Uncertain;
            string units = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValue.UtcSampleTime), StringComparison.OrdinalIgnoreCase)) {
                    utcSampleTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValue.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValue.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<TagValueStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValue.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new TagValue(utcSampleTime, value, status, units);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValue value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValue.UtcSampleTime), value.UtcSampleTime, options);
            WritePropertyValue(writer, nameof(TagValue.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValue.Status), value.Status, options);
            WritePropertyValue(writer, nameof(TagValue.Units), value.Units, options);
            writer.WriteEndObject();
        }

    }

}
