using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Helper class for constructing <see cref="TagValue"/> objects using a fluent interface.
    /// </summary>
    public class TagValueBuilder {

        /// <summary>
        /// The UTC sample time.
        /// </summary>
        private DateTime _utcSampleTime = DateTime.UtcNow;

        /// <summary>
        /// The numeric value.
        /// </summary>
        private double _numericValue = double.NaN;

        /// <summary>
        /// The text value.
        /// </summary>
        private string _textValue;

        /// <summary>
        /// The quality status.
        /// </summary>
        private TagValueStatus _status = TagValueStatus.Unknown;

        /// <summary>
        /// The units.
        /// </summary>
        private string _units;

        /// <summary>
        /// Notes associated with the value.
        /// </summary>
        private string _notes;

        /// <summary>
        /// Error message associated with the value.
        /// </summary>
        private string _error;

        /// <summary>
        /// Bespoke tag value properties.
        /// </summary>
        private readonly Dictionary<string, string> _properties;


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object.
        /// </summary>
        private TagValueBuilder() {
            _properties = new Dictionary<string, string>();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object that is initialised using an existing 
        /// tag value.
        /// </summary>
        /// <param name="existing">
        ///   The existing value.
        /// </param>
        private TagValueBuilder(TagValue existing) {
            if (existing == null) {
                _properties = new Dictionary<string, string>();
                return;
            }

            _utcSampleTime = existing.UtcSampleTime;
            _numericValue = existing.NumericValue;
            _textValue = existing.TextValue;
            _status = existing.Status;
            _units = existing.Units;
            _notes = existing.Notes;
            _error = existing.Error;
            _properties = new Dictionary<string, string>(existing.Properties);
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


        /// <summary>
        /// Creates a <see cref="TagValue"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValue"/> object.
        /// </returns>
        public TagValue Build() {
            return TagValue.Create(_utcSampleTime, _numericValue, _textValue, _status, _units, _notes, _error, _properties);
        }


        /// <summary>
        /// Infrastructure only. <see cref="TagDefinitionExtensions.GetTextValue(TagDefinition, double, IFormatProvider)"/> 
        /// should be used instead.
        /// </summary>
        /// <param name="numericValue">
        ///   The numeric value.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use.
        /// </param>
        /// <returns>
        ///   The equivalent text value.
        /// </returns>
        internal static string GetTextValue(double numericValue, IFormatProvider provider = null) {
            return numericValue.ToString(provider ?? CultureInfo.CurrentCulture);
        }


        /// <summary>
        /// Updates the UTC sample time for the value.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithUtcSampleTime(DateTime utcSampleTime) {
            _utcSampleTime = utcSampleTime.ToUniversalTime();
            return this;
        }


        /// <summary>
        /// Updates both the numeric and text values. If <paramref name="textValue"/> is 
        /// <see langword="null"/> or white space, it will be updated by converting 
        /// <paramref name="numericValue"/> to a string.
        /// </summary>
        /// <param name="numericValue">
        ///   The numeric value.
        /// </param>
        /// <param name="textValue">
        ///   The text value.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use if a conversion of the numeric value to a string is required.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithValues(double numericValue, string textValue, IFormatProvider provider = null) {
            _numericValue = numericValue;
            _textValue = string.IsNullOrWhiteSpace(textValue)
                ? GetTextValue(numericValue, provider)
                : textValue;
            return this;
        }


        /// <summary>
        /// Updates the numeric value. If the text value is currently <see langword="null"/> or white 
        /// space, it will be updated as a string-formatted version of the numeric value.
        /// </summary>
        /// <param name="value">
        ///   The numeric value.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithNumericValue(double value, IFormatProvider provider = null) {
            _numericValue = value;
            if (string.IsNullOrWhiteSpace(_textValue)) {
                _textValue = GetTextValue(value, provider);
            }
            return this;
        }


        /// <summary>
        /// Updates the text value. If the numeric value is currently <see cref="double.NaN"/> and 
        /// the text value can be parsed to a <see cref="double"/>, the numeric value will also be 
        /// updated.
        /// </summary>
        /// <param name="value">
        ///   The text value.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithTextValue(string value, IFormatProvider provider = null) {
            _textValue = value;
            if (!string.IsNullOrWhiteSpace(_textValue) && double.IsNaN(_numericValue) && double.TryParse(value, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture, out var numericValue)) {
                _numericValue = numericValue;
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
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithStatus(TagValueStatus status) {
            _status = status;
            return this;
        }


        /// <summary>
        /// Updates the units.
        /// </summary>
        /// <param name="units">
        ///   The updated units.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithUnits(string units) {
            _units = units;
            return this;
        }


        /// <summary>
        /// Updates the notes.
        /// </summary>
        /// <param name="notes">
        ///   The updated notes.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithNotes(string notes) {
            _notes = notes;
            return this;
        }


        /// <summary>
        /// Updates the error message.
        /// </summary>
        /// <param name="error">
        ///   The updated error message.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithError(string error) {
            _error = error;
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
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithProperty(string name, string value) {
            if (name != null) {
                _properties[name] = value;
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
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithProperties(IEnumerable<KeyValuePair<string, string>> properties) {
            if (properties != null) {
                foreach (var prop in properties) {
                    if (prop.Key != null) {
                        _properties[prop.Key] = prop.Value;
                    }
                }
            }
            return this;
        }

    }
}
