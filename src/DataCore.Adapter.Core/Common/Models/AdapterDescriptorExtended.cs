using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// An extended descriptor for an adapter, that includes information about the features that the 
    /// adapter has implemented.
    /// </summary>
    [Serializable]
    public class AdapterDescriptorExtended: AdapterDescriptor, ISerializable {

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
        public IDictionary<string, string> Properties { get; }


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
        public AdapterDescriptorExtended(string id, string name, string description, IEnumerable<string> features, IEnumerable<string> extensions, IDictionary<string, string> properties): base(id, name, description) {
            Features = features?.ToArray() ?? new string[0];
            Extensions = extensions?.ToArray() ?? new string[0];
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorExtended"/> object from the provided serialization 
        /// information.
        /// </summary>
        /// <param name="info">
        ///   The serialization information.
        /// </param>
        /// <param name="context">
        ///   The streaming context.
        /// </param>
        public AdapterDescriptorExtended(SerializationInfo info, StreamingContext context): base(info?.GetString("id"), info?.GetString("name"), info?.GetString("description")) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }

            Features = (string[]) info?.GetValue("features", typeof(string[])) ?? new string[0];
            Extensions = (string[]) info?.GetValue("extensions", typeof(string[])) ?? new string[0];
            Properties = new ReadOnlyDictionary<string, string>((Dictionary<string, string>) info?.GetValue("properties", typeof(Dictionary<string, string>)) ?? new Dictionary<string, string>());
        }


        /// <summary>
        /// Adds serialization information to the provided serialization information object.
        /// </summary>
        /// <param name="info">
        ///   The serialization information.
        /// </param>
        /// <param name="context">
        ///   The streaming context.
        /// </param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("id", Id);
            info.AddValue("name", Name);
            info.AddValue("description", Description);
            info.AddValue("features", Features.ToArray(), typeof(string[]));
            info.AddValue("extensions", Extensions.ToArray(), typeof(string[]));
            info.AddValue("properties", new Dictionary<string, string>(Properties), typeof(Dictionary<string, string>));
        }
    }
}
