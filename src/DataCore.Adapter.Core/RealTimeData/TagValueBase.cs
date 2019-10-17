using System;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the base set of properties for a tag value.
    /// </summary>
    public class TagValueBase {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; set; }

        /// <summary>
        /// The tag value.
        /// </summary>
        public Variant Value { get; set; }

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
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <param name="status">
        ///   The quality status for the value.
        /// </param>
        /// <param name="units">
        ///   The value units.
        /// </param>
        public static TagValueBase Create(DateTime utcSampleTime, Variant value, TagValueStatus status, string units) {
            return new TagValueBase() {
                UtcSampleTime = utcSampleTime,
                Value = value,
                Status = status,
                Units = units ?? string.Empty
            };
        }

    }
}
