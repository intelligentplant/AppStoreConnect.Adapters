using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a real-time or historical value on a tag.
    /// </summary>
    public sealed class TagValue : TagValueBase {

        /// <summary>
        /// Notes associated with the value.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// An error message associated with the value.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Additional value properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> object.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <param name="numericValue">
        ///   The numeric tag value.
        /// </param>
        /// <param name="textValue">
        ///   The text tag value.
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
        public static TagValue Create(DateTime utcSampleTime, double numericValue, string textValue, TagValueStatus status, string units, string notes, string error, IEnumerable<AdapterProperty> properties) {
            return new TagValue() {
                UtcSampleTime = utcSampleTime,
                NumericValue = numericValue,
                TextValue = textValue,
                Status = status,
                Units = units ?? string.Empty,
                Notes = notes,
                Error = error,
                Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>()
            };
        }

    }
}
