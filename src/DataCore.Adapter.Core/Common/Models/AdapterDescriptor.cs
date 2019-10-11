using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// A descriptor for an App Store Connect adapter.
    /// </summary>
    public class AdapterDescriptor : IAdapterDescriptor {

        /// <summary>
        /// The identifier for the adapter. This can be any type of value, as long as it is unique 
        /// within the hosting application, and does not change.
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// The adapter name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        public string Description { get; set; }


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
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public static AdapterDescriptor Create(string id, string name, string description) {
            return new AdapterDescriptor() {
                Id = string.IsNullOrWhiteSpace(id)
                    ? throw new ArgumentException(SharedResources.Error_AdapterDescriptorIdIsRequired, nameof(id))
                    : id,
                Name = string.IsNullOrWhiteSpace(name)
                    ? throw new ArgumentException(SharedResources.Error_AdapterDescriptorNameIsRequired, nameof(name))
                    : name,
                Description = description
            };
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
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
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
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        public static AdapterDescriptor Create(string id) {
            return Create(id, id, null);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptor"/> from an existing 
        /// <see cref="IAdapterDescriptor"/>.
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
        public static AdapterDescriptor FromExisting(IAdapterDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return Create(descriptor.Id, descriptor.Name, descriptor.Description);
        }

    }
}
