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
        public IEnumerable<string> Features { get; }

        /// <summary>
        /// The names of the implemented extension adapter features.
        /// </summary>
        public IEnumerable<string> Extensions { get; }

        /// <summary>
        /// Additional adapter properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


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
        public AdapterDescriptorExtended(
            string id, 
            string name, 
            string description, 
            IEnumerable<string> features, 
            IEnumerable<string> extensions, 
            IEnumerable<AdapterProperty> properties
        ) : base(id, name, description) {
            Features = features?.ToArray() ?? Array.Empty<string>();
            Extensions = extensions?.ToArray() ?? Array.Empty<string>();
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


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
        public static AdapterDescriptorExtended Create(string id, string name, string description, IEnumerable<string> features, IEnumerable<string> extensions, IEnumerable<AdapterProperty> properties) {
            return new AdapterDescriptorExtended(id, name, description, features, extensions, properties);
        }

    }
}
