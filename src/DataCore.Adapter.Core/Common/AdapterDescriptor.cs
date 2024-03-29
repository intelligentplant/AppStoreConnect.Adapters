﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// A descriptor for an App Store Connect adapter.
    /// </summary>
    public class AdapterDescriptor {

        /// <summary>
        /// The maximum length of an adapter <see cref="Id"/>.
        /// </summary>
        public const int IdMaxLength = 200;

        /// <summary>
        /// The identifier for the adapter. This can be any type of value, as long as it is unique 
        /// within the hosting application, and does not change.
        /// </summary>
        [Required]
        [MaxLength(IdMaxLength)]
        public string Id { get; }

        /// <summary>
        /// The adapter name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        public string? Description { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/>, white space or longer than <see cref="IdMaxLength"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        [JsonConstructor]
        public AdapterDescriptor(string id, string name, string? description) {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentOutOfRangeException(nameof(id), SharedResources.Error_IdIsRequired)
                : id.Length > IdMaxLength
                    ? throw new ArgumentOutOfRangeException(nameof(id), string.Format(CultureInfo.CurrentCulture, SharedResources.Error_IdIsTooLong, IdMaxLength))
                    : id;
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentOutOfRangeException(nameof(name), SharedResources.Error_NameIsRequired)
                : name;
            Description = description;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> object using the specified ID, name and 
        /// description.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/>, white space or longer than <see cref="IdMaxLength"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        [Obsolete("Use constructor instead.", true)]
        public static AdapterDescriptor Create(string id, string name, string? description) {
            return new AdapterDescriptor(id, name, description);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> object using the specified ID and name.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/>, white space or longer than <see cref="IdMaxLength"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        [Obsolete("Use constructor instead.", true)]
        public static AdapterDescriptor Create(string id, string name) {
            return Create(id, name, null);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> object using the specified ID. The ID 
        /// will also be used as the adapter name.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/>, white space or longer than <see cref="IdMaxLength"/>.
        /// </exception>
        [Obsolete("Use constructor instead.", true)]
        public static AdapterDescriptor Create(string id) {
            return Create(id, id, null);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> from an existing instance.
        /// </summary>
        /// <param name="descriptor">
        ///   The existing descriptor.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterDescriptor"/> object.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="descriptor"/> is <see langword="null"/> or white space.
        /// </exception>
        public static AdapterDescriptor FromExisting(AdapterDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new AdapterDescriptor(descriptor.Id, descriptor.Name, descriptor.Description);
        }

    }

}
