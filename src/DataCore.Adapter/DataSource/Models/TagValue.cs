using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes a real-time or historical value on a tag.
    /// </summary>
    public sealed class TagValue {

        /// <summary>
        /// The UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; private set; }

        /// <summary>
        /// The numeric value for the tag. This can differ from the text value on state-based and 
        /// non-numeric tags.
        /// </summary>
        public double NumericValue { get; private set; } = double.NaN;

        /// <summary>
        /// The text value for the tag. This can differ from the numeric value on state-based and 
        /// non-numeric tags.
        /// </summary>
        public string TextValue { get; private set; }

        /// <summary>
        /// The quality status for the value.
        /// </summary>
        public TagValueStatus Status { get; private set; } = TagValueStatus.Unknown;

        /// <summary>
        /// The value units.
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// Notes associated with the value.
        /// </summary>
        public string Notes { get; private set; }

        /// <summary>
        /// An error message associated with the value.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Additional value properties.
        /// </summary>
        private readonly Dictionary<string, string> _properties;

        /// <summary>
        /// Additional value properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; private set; }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> object with good <see cref="Status"/> and the 
        /// <see cref="UtcSampleTime"/> set to the current UTC time.
        /// </summary>
        private TagValue() {
            UtcSampleTime = DateTime.UtcNow;
            Status = TagValueStatus.Good;
        }


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
        public TagValue(DateTime utcSampleTime, double numericValue, string textValue, TagValueStatus status, string units, string notes, string error, IDictionary<string, string> properties): this() {
            UtcSampleTime = utcSampleTime.ToUniversalTime();
            NumericValue = numericValue;
            TextValue = textValue;
            Status = status;
            Units = units;
            Notes = notes;
            Error = error;
            _properties = properties == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(properties);
            Properties = new ReadOnlyDictionary<string, string>(_properties);
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
        /// Creates a new <see cref="TagValue"/> with the time stamp set to the current UTC time.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValue"/> object.
        /// </returns>
        public static TagValue Create() {
            return new TagValue();
        }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> that is a copy of the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value to copy.
        /// </param>
        /// <returns>
        ///   A copy of the original value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static TagValue CreateFromExisting(TagValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            return new TagValue(value.UtcSampleTime, value.NumericValue, value.TextValue, value.Status, value.Units, value.Notes, value.Error, value._properties);
        }


        /// <summary>
        /// Updates the UTC sample time for the value.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithUtcSampleTime(DateTime utcSampleTime) {
            UtcSampleTime = utcSampleTime.ToUniversalTime();
            return this;
        }


        /// <summary>
        /// Updates the numeric value.
        /// </summary>
        /// <param name="value">
        ///   The numeric value.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithNumericValue(double value) {
            NumericValue = value;
            if (String.IsNullOrWhiteSpace(TextValue)) {
                TextValue = GetTextValue(value);
            }
            return this;
        }


        /// <summary>
        /// Updates the text value.
        /// </summary>
        /// <param name="value">
        ///   The text value.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithTextValue(string value) {
            TextValue = value;
            if (!String.IsNullOrWhiteSpace(TextValue) && double.IsNaN(NumericValue) && double.TryParse(value, out var numericValue)) {
                NumericValue = numericValue;
            }
            return this;
        }


        /// <summary>
        /// Updates the quality status.
        /// </summary>
        /// <param name="status">
        ///   The updated status.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithStatus(TagValueStatus status) {
            Status = status;
            return this;
        }


        /// <summary>
        /// Updates the units.
        /// </summary>
        /// <param name="units">
        ///   The updated units.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithUnits(string units) {
            Units = units;
            return this;
        }


        /// <summary>
        /// Updates the notes.
        /// </summary>
        /// <param name="notes">
        ///   The updated notes.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithNotes(string notes) {
            Notes = notes;
            return this;
        }


        /// <summary>
        /// Updates the error message.
        /// </summary>
        /// <param name="error">
        ///   The updated error message.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithError(string error) {
            Error = error;
            return this;
        }


        /// <summary>
        /// Adds a property to the tag value.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithProperty(string name, string value) {
            if (name != null) {
                _properties[name] = value;
                Properties = new ReadOnlyDictionary<string, string>(_properties);
            }
            return this;
        }


        /// <summary>
        /// Adds a set of properties to the tag value.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated tag value.
        /// </returns>
        public TagValue WithProperties(IDictionary<string, string> properties) {
            if (properties != null) {
                var dirty = false;
                foreach (var prop in properties) {
                    dirty = true;
                    _properties[prop.Key] = prop.Value;
                }

                if (dirty) {
                    Properties = new ReadOnlyDictionary<string, string>(_properties);
                }
            }
            return this;
        }

    }
}
