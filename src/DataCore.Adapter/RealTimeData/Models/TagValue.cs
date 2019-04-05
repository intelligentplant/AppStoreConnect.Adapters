using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a real-time or historical value on a tag. Use the <see cref="Create"/> or <see cref="CreateFromExisting(TagValue)"/> 
    /// methods to build new values using a fluent interface.
    /// </summary>
    /// <seealso cref="TagValueBuilder"/>
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
        /// Creates a new <see cref="TagValue"/> object. The static <see cref="Create"/> and 
        /// <see cref="CreateFromExisting(TagValue)"/> methods are also avalable, for easier 
        /// construction using a fluent interface.
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


        /// <summary>
        /// Infrastructure only. <see cref="TagDefinitionExtensions.GetTextValue(TagDefinition, double)"/> 
        /// should be used instead.
        /// </summary>
        /// <param name="numericValue">
        ///   The numeric value.
        /// </param>
        /// <returns>
        ///   The equivalent text value.
        /// </returns>
        internal static string GetTextValue(double numericValue) {
            return numericValue.ToString(CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValueBuilder"/> object.
        /// </returns>
        public static TagValueBuilder Create() {
            return new TagValueBuilder();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> that is configured using an existing 
        /// tag value.
        /// </summary>
        /// <param name="other">
        ///   The tag value to copy the initial values from.
        /// </param>
        /// <returns>
        ///   An <see cref="TagValueBuilder"/> with pre-configured properties.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueBuilder CreateFromExisting(TagValue other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new TagValueBuilder(other);
        }

    }
}
