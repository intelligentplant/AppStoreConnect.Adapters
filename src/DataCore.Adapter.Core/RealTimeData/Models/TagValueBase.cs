using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes the base set of properties for a tag value.
    /// </summary>
    public class TagValueBase {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; }

        /// <summary>
        /// The numeric value for the tag. This can differ from the text value on state-based and 
        /// non-numeric tags.
        /// </summary>
        public double NumericValue { get; }

        /// <summary>
        /// The text value for the tag. This can differ from the numeric value on state-based and 
        /// non-numeric tags.
        /// </summary>
        public string TextValue { get; }

        /// <summary>
        /// The quality status for the value.
        /// </summary>
        public TagValueStatus Status { get; }


        /// <summary>
        /// Creates a new <see cref="TagValueBase"/> object.
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
        public TagValueBase(DateTime utcSampleTime, double numericValue, string textValue, TagValueStatus status) {
            UtcSampleTime = utcSampleTime;
            NumericValue = numericValue;
            TextValue = textValue;
            Status = status;
        }

    }
}
