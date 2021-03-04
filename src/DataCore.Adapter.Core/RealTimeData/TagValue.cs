using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the base set of properties for a tag value sample.
    /// </summary>
    public class TagValue : IFormattable {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; }

        /// <summary>
        /// The values for the sample. In the majority of cases, this collection will contain a 
        /// single item. However, multiple items are allowed to account for situations where the 
        /// sample represents a digital state, and both the numerical and text values of the state 
        /// are being returned.
        /// </summary>
        public IEnumerable<Variant> Values { get; }

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
        /// <param name="values">
        ///   The values for the sample. In the majority of cases, this collection will contain a 
        ///   single item. However, multiple items are allowed to account for situations where the 
        ///   sample represents a digital state, and both the numerical and text values of the state 
        ///   are being returned.
        /// </param>
        /// <param name="status">
        ///   The quality status for the value.
        /// </param>
        /// <param name="units">
        ///   The value units.
        /// </param>
        public TagValue(DateTime utcSampleTime, IEnumerable<Variant>? values, TagValueStatus status, string? units) {
            UtcSampleTime = utcSampleTime;
            Values = values?.ToArray() ?? Array.Empty<Variant>();
            if (!Values.Any()) {
                throw new ArgumentException(SharedResources.Error_AtLeastOneValueIsRequired, nameof(values));
            }
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
            return new TagValue(utcSampleTime, new[] { value }, status, units);
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
            var val = Values.First();
            var formattedValue = val.ToString(format ?? Variant.GetDefaultFormat(val.Type), formatProvider);
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
