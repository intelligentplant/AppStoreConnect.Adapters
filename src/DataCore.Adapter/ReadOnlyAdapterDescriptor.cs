using System;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter {

    /// <summary>
    /// Read-only <see cref="IAdapterDescriptor"/> implementation.
    /// </summary>
    internal class ReadOnlyAdapterDescriptor : IAdapterDescriptor {

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string Description { get; }


        /// <summary>
        /// Creates a new <see cref="ReadOnlyAdapterDescriptor"/> object
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
        internal ReadOnlyAdapterDescriptor(string id, string name, string description) {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException(SharedResources.Error_AdapterDescriptorIdIsRequired, nameof(id))
                : id;
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(SharedResources.Error_AdapterDescriptorNameIsRequired, nameof(name))
                : name;
            Description = description;
        }

    }
}
