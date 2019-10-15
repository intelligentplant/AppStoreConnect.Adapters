using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// An extended descriptor for an adapter, that includes information about the features that the 
    /// adapter has implemented.
    /// </summary>
    public class AdapterDescriptorExtended : AdapterDescriptor {

        /// <summary>
        /// The names of the implemented standard adapter features.
        /// </summary>
        public IEnumerable<string> Features { get; set; }

        /// <summary>
        /// The names of the implemented extension adapter features.
        /// </summary>
        public IEnumerable<string> Extensions { get; set; }

        /// <summary>
        /// Additional adapter properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorExtended"/> object.
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
        /// <param name="features">
        ///   The standard features implemented by the adapter, typically the simple name of the 
        ///   feature type.
        /// </param>
        /// <param name="extensions">
        ///   The extension features implemented by the adapter, typically the namespace-qualified name 
        ///   of the feature type.
        /// </param>
        /// <param name="properties">
        ///   Additional adapter properties.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public static AdapterDescriptorExtended Create(string id, string name, string description, IEnumerable<string> features, IEnumerable<string> extensions, IDictionary<string, string> properties) {
            return new AdapterDescriptorExtended() {
                Id = string.IsNullOrWhiteSpace(id)
                    ? throw new ArgumentException(SharedResources.Error_AdapterDescriptorIdIsRequired, nameof(id))
                    : id,
                Name = string.IsNullOrWhiteSpace(name)
                    ? throw new ArgumentException(SharedResources.Error_AdapterDescriptorNameIsRequired, nameof(name))
                    : name,
                Description = description,
                Features = features?.ToArray() ?? Array.Empty<string>(),
                Extensions = extensions?.ToArray() ?? Array.Empty<string>(),
                Properties = properties ?? new Dictionary<string, string>()
            };
        }

    }
}
