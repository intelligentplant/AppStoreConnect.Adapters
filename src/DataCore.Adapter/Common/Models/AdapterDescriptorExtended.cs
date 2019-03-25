using System;
using System.Collections.Generic;
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
        /// <see cref="IAdapterFeature"/> type.
        /// </summary>
        private static readonly Type s_adapterFeatureType = typeof(IAdapterFeature);

        /// <summary>
        /// The names of the implemented adapter features.
        /// </summary>
        public IEnumerable<string> Features { get; }


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
        ///   The adapter feature names.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptorExtended(string id, string name, string description, IEnumerable<string> features): base(id, name, description) {
            Features = features?.ToArray() ?? new string[0];
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorExtended"/> object for an <see cref="IAdapter"/>.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public AdapterDescriptorExtended(IAdapter adapter) : base(adapter?.Descriptor) {
            Features = adapter?.Features?.Keys?.Where(x => s_adapterFeatureType.IsAssignableFrom(x)).OrderBy(x => x.Name).Select(x => x.Name).ToArray() ?? throw new ArgumentNullException(nameof(adapter));
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
        public AdapterDescriptorExtended(SerializationInfo info, StreamingContext context): base(info?.GetString(nameof(Id)), info?.GetString(nameof(Name)), info?.GetString(nameof(Description))) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }

            Features = (string[]) info?.GetValue(nameof(Features), typeof(string[])) ?? new string[0];
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

            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Description), Description);
            info.AddValue(nameof(Features), Features.ToArray(), typeof(string[]));
        }
    }
}
