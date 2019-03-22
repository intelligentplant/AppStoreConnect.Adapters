using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// A descriptor for an App Store Connect adapter.
    /// </summary>
    public class AdapterDescriptor {

        /// <summary>
        /// The identifier for the adapter. This can be any type of value, as long as it is unique 
        /// within the hosting application, and does not change.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The adapter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        public string Description { get; }


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
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptor(string id, string name, string description) {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException(Resources.Error_AdapterDescriptorIdIsRequired, nameof(id))
                : id;
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(Resources.Error_AdapterDescriptorNameIsRequired, nameof(name))
                : name;
            Description = description;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> that is a copy of an existing descriptor.
        /// </summary>
        /// <param name="other">
        ///   The descriptor to copy from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>
        /// </exception>
        public AdapterDescriptor(AdapterDescriptor other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            Id = other.Id;
            Name = other.Name;
            Description = other.Description;
        }

    }
}
