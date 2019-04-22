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
        }
    }
}
