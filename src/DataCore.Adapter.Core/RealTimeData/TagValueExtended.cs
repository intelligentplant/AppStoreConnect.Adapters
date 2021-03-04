using System;
using System.Collections.Generic;
using System.Linq;
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
            IEnumerable<Variant> values,
            TagValueStatus status, 
            string? units, 
            string? notes, 
            string? error, 
            IEnumerable<AdapterProperty>? properties
        ) : base(utcSampleTime, values, status, units) {
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
            return new TagValueExtended(utcSampleTime, new[] { value }, status, units, notes, error, properties);
        }

    }
}
