using System;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes the base set of properties for a tag value.
    /// </summary>
    public class TagValueBase {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; set; }

        /// <summary>
        /// The numeric value for the tag. This can differ from the text value on state-based and 
        /// non-numeric tags.
        /// </summary>
        public double NumericValue { get; set; }

        /// <summary>
        /// The text value for the tag. This can differ from the numeric value on state-based and 
        /// non-numeric tags.
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// The quality status for the value.
        /// </summary>
        public TagValueStatus Status { get; set; }

        /// <summary>
        /// The value units.
        /// </summary>
        public string Units { get; set; }


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
        /// <param name="units">
        ///   The value units.
        /// </param>
        public static TagValueBase Create(DateTime utcSampleTime, double numericValue, string textValue, TagValueStatus status, string units) {
            return new TagValueBase() {
                UtcSampleTime = utcSampleTime,
                NumericValue = numericValue,
                TextValue = textValue,
                Status = status,
                Units = units ?? string.Empty
            };
        }

    }
}
