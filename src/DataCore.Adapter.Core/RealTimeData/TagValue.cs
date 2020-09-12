using System;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the base set of properties for a tag value.
    /// </summary>
    public class TagValue : IFormattable {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; }

        /// <summary>
        /// The tag value.
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
        ///   The tag value.
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
            var formattedValue = Value.ToString(format ?? Variant.GetDefaultFormat(Value.Type), formatProvider);
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
}
