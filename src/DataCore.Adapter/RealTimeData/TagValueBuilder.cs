﻿using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Helper class for constructing <see cref="TagValueExtended"/> objects using a fluent interface.
    /// </summary>
    public sealed class TagValueBuilder : AdapterEntityBuilder<TagValueExtended> {

        /// <summary>
        /// The UTC sample time.
        /// </summary>
        private DateTime? _utcSampleTime;

        /// <summary>
        /// The value for the sample.
        /// </summary>
        private Variant _value = Variant.Null;

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
        [Obsolete("This method will be removed in a future release. Use TagValueBuilder() instead.", false)]
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
        [Obsolete("This method will be removed in a future release. Use TagValueBuilder(TagValueExtended) instead.", false)]
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
        public override TagValueExtended Build() {
            return new TagValueExtended(_utcSampleTime ??= DateTime.UtcNow, _value, _status, _units, _notes, _error, GetProperties());
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
            this.RemoveProperty(WellKnownProperties.TagValue.DisplayValue);
            if (displayValue != null) {
                return this.WithProperty(WellKnownProperties.TagValue.DisplayValue.InternToStringCache(), displayValue);
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
            _units = string.IsNullOrWhiteSpace(units)
                ? units
                : units!.InternToStringCache();
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

    }
}
