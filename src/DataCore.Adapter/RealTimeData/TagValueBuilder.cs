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
        /// The value.
        /// </summary>
        private Variant _value = Variant.Null;

        /// <summary>
        /// Additional values (e.g. the name of a digital state if <see cref="_value"/> represents 
        /// the state's value).
        /// </summary>
        private readonly List<Variant> _additionalValues = new List<Variant>();

        /// <summary>
        /// The quality status.
        /// </summary>
        private TagValueStatus _status = TagValueStatus.Good;

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
        public TagValueBuilder(TagValueExtended existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithUtcSampleTime(existing.UtcSampleTime);
            WithValue(existing.Value);
            WithAdditionalValues(existing.AdditionalValues);
            WithStatus(existing.Status);
            WithNotes(existing.Notes);
            WithError(existing.Error);
            WithProperties(existing.Properties);
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
            return new TagValueExtended(_utcSampleTime, _value, _additionalValues, _status, _units, _notes, _error, _properties);
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
        /// Updates the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithValue(Variant value) {
            _value = value;
            return this;
        }


        /// <summary>
        /// Updates the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public TagValueBuilder WithValue<T>(T value) {
            _value = Variant.FromValue(value);
            return this;
        }


        /// <summary>
        /// Adds secondary values.
        /// </summary>
        /// <param name="values">
        ///   The additional values.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   Secondary values can be used when e.g. the primary value is the name of a digital 
        ///   state, but you also want to provide the state's numeric value.
        /// </remarks>
        public TagValueBuilder WithAdditionalValues(params Variant[] values) {
            return WithAdditionalValues((IEnumerable<Variant>) values);
        }


        /// <summary>
        /// Adds secondary values.
        /// </summary>
        /// <param name="values">
        ///   The additional values.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   Secondary values can be used when e.g. the primary value is the name of a digital 
        ///   state, but you also want to provide the state's numeric value.
        /// </remarks>
        public TagValueBuilder WithAdditionalValues(IEnumerable<Variant> values) {
            if (values != null) {
                _additionalValues.AddRange(values);
            }
            return this;
        }


        /// <summary>
        /// Adds a secondary value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value.
        /// </typeparam>
        /// <param name="value">
        ///   The additional value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   Secondary values can be used when e.g. the primary value is the name of a digital 
        ///   state, but you also want to provide the state's numeric value.
        /// </remarks>
        public TagValueBuilder WithAdditionalValue<T>(T value) {
            _additionalValues.Add(Variant.FromValue(value));
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
        /// <remarks>
        ///   If <paramref name="error"/> is not <see langword="null"/> or white space, the status 
        ///   of the value will also be set to <see cref="TagValueStatus.Bad"/>.
        /// </remarks>
        public TagValueBuilder WithError(string? error) {
            _error = error;
            if (!string.IsNullOrWhiteSpace(error)) {
                _status = TagValueStatus.Bad;
            }
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
