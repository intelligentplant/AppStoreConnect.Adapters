using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a real-time or historical value on a tag.
    /// </summary>
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
        [JsonConstructor]
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

    }

}
