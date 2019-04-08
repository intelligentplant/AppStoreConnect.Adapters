using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a real-time or historical value on a tag.
    /// </summary>
    public sealed class TagValue : TagValueBase {

        /// <summary>
        /// The value units.
        /// </summary>
        public string Units { get; }

        /// <summary>
        /// Notes associated with the value.
        /// </summary>
        public string Notes { get; }

        /// <summary>
        /// An error message associated with the value.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Additional value properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; }


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
        public TagValue(DateTime utcSampleTime, double numericValue, string textValue, TagValueStatus status, string units, string notes, string error, IDictionary<string, string> properties) : base(utcSampleTime, numericValue, textValue, status) {
            Units = units;
            Notes = notes;
            Error = error;
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }

    }
}
