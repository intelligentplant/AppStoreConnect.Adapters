using System;
using System.Collections.Generic;
using System.Linq;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Helper class for constructing <see cref="TagValueExtended"/> objects using a fluent interface.
    /// </summary>
    public class TagValueBuilder {

        /// <summary>
        /// The UTC sample time.
        /// </summary>
        private DateTime _utcSampleTime = DateTime.UtcNow;

        /// <summary>
        /// The value for the sample.
        /// </summary>
        private Variant _value = Variant.Null;

        /// <summary>
        /// The status code.
        /// </summary>
        private StatusCode _status = StatusCode.ForTagValue(StatusCodes.Good, TagValueInfoBits.None);

        /// <summary>
        /// The units.
        /// </summary>
        private string? _units;

        /// <summary>
        /// Notes associated with the value.
        /// </summary>
        private string? _notes;

        /// <summary>
        /// Error message associated with the value.
        /// </summary>
        private string? _error;

        /// <summary>
        /// Bespoke tag value properties.
        /// </summary>
        private readonly List<AdapterProperty> _properties = new List<AdapterProperty>();


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object.
        /// </summary>
        public TagValueBuilder() { }


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object that is initialised using an existing 
        /// tag value.
        /// </summary>
        /// <param name="existing">
        ///   The existing value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public TagValueBuilder(TagValueExtended existing) : this((TagValue) existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithUtcSampleTime(existing.UtcSampleTime);
            WithValue(existing.Value);
            WithStatus(existing.Status);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object that is initialised using an existing 
        /// tag value.
        /// </summary>
        /// <param name="existing">
        ///   The existing value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public TagValueBuilder(TagValue existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithUtcSampleTime(existing.UtcSampleTime);
            WithValue(existing.Value);
            WithStatus(existing.Status);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueBuilder"/> object.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValueBuilder"/> object.
        /// </returns>
        [Obsolete("Use TagValueBuilder() constructor", false)]
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
        [Obsolete("Use TagValueBuilder(TagValueExtended) constructor", false)]
        public static TagValueBuilder CreateFromExisting(TagValueExtended other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new TagValueBuilder(other);
        }


        /// <summary>
        /// Creates a <see cref="TagValueExtended"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValueExtended"/> object.
        /// </returns>
        public TagValueExtended Build() {
            return new TagValueExtended(_utcSampleTime, _value, _status, _units, _notes, _error, _properties);
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
        /// Sets the value for the sample.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="displayValue">
        ///   The display value for the sample. Specifying a display value adds a 
        ///   <see cref="WellKnownProperties.TagValue.DisplayValue"/> property to the sample.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithValue(Variant value, string? displayValue = null) {
            _value = value;
            var existingDisplayValue = _properties.RemoveAll(x => x.Name.Equals(WellKnownProperties.TagValue.DisplayValue, StringComparison.OrdinalIgnoreCase));
            if (displayValue != null) {
                return WithProperty(WellKnownProperties.TagValue.DisplayValue, displayValue);
            }
            return this;
        }


        /// <summary>
        /// Sets the value for the sample.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="displayValue">
        ///   The display value for the sample. Specifying a display value adds a 
        ///   <see cref="WellKnownProperties.TagValue.DisplayValue"/> property to the sample.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithValue<T>(T value, string? displayValue = null) {
            return WithValue(Variant.FromValue(value), displayValue);
        }


        /// <summary>
        /// Updates the status code.
        /// </summary>
        /// <param name="status">
        ///   The updated status.
        /// </param>
        /// <param name="infoBits">
        ///   The info bits for the tag value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   The <see cref="StatusCodes"/> class defines constants for the most common status 
        ///   codes.
        /// </remarks>
        public TagValueBuilder WithStatus(StatusCode status, TagValueInfoBits infoBits = TagValueInfoBits.None) {
            _status = StatusCode.ForTagValue(status, infoBits);
            return this;
        }


        /// <summary>
        /// Updates the status code.
        /// </summary>
        /// <param name="status">
        ///   The updated status.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        [Obsolete("TagValueStatus is deprecated. Use WithStatus(StatusCode) instead.", false)]
        public TagValueBuilder WithStatus(TagValueStatus status) {
            _status = StatusCode.FromTagValueStatus(status);
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
        public TagValueBuilder WithUnits(string? units) {
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
        public TagValueBuilder WithNotes(string? notes) {
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
        public TagValueBuilder WithError(string? error) {
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
        public TagValueBuilder WithProperty(string name, object value) {
            if (name != null) {
                _properties.Add(AdapterProperty.Create(name, value));
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
        public TagValueBuilder WithProperties(params AdapterProperty[] properties) {
            return WithProperties((IEnumerable<AdapterProperty>) properties);
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
        public TagValueBuilder WithProperties(IEnumerable<AdapterProperty> properties) {
            if (properties != null) {
                _properties.AddRange(properties.Where(x => x != null));
            }
            return this;
        }


        /// <summary>
        /// Adds a set of properties to the tag value being calculated from a bucket.
        /// </summary>
        /// <param name="bucket">
        ///   The bucket.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        internal TagValueBuilder WithBucketProperties(TagValueBucket bucket) {
            if (bucket != null) {
                return WithProperties(
                    AdapterProperty.Create(CommonTagPropertyNames.BucketStart, bucket.UtcBucketStart),
                    AdapterProperty.Create(CommonTagPropertyNames.BucketEnd, bucket.UtcBucketEnd)
                );
            }

            return this;
        }

    }
}
