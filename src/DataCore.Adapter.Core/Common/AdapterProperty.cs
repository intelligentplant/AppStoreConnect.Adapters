﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a custom property associated with an adapter, tag, tag value, event message, etc.
    /// </summary>
    public class AdapterProperty {

        /// <summary>
        /// The property name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The property value.
        /// </summary>
        public Variant Value { get; }

        /// <summary>
        /// The property description.
        /// </summary>
        public string? Description { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        [JsonConstructor]
        public AdapterProperty(string name, Variant value, string? description = null) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            Description = description;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty Create(string name, Variant value, string? description = null) {
            return new AdapterProperty(name, value, description);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty Create(string name, object value, string? description = null) {
            return new AdapterProperty(name, Variant.FromValue(value), description);
        }


        /// <summary>
        /// Creates a copy of an existing <see cref="AdapterProperty"/>.
        /// </summary>
        /// <param name="property">
        ///   The property to clone.
        /// </param>
        /// <returns>
        ///   A copy of the property.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty FromExisting(AdapterProperty property) {
            if (property == null) {
                throw new ArgumentNullException(nameof(property));
            }

            return Create(property.Name, property.Value, property.Description);
        }

    }

}
