using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// An extended descriptor for an adapter, that includes information about the features that the 
    /// adapter has implemented.
    /// </summary>
    public class AdapterDescriptorExtended: AdapterDescriptor {

        /// <summary>
        /// <see cref="IAdapterFeature"/> type.
        /// </summary>
        private static readonly Type s_adapterFeatureType = typeof(IAdapterFeature);

        /// <summary>
        /// The names of the implemented adapter features.
        /// </summary>
        public IEnumerable<string> Features { get; }


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

    }
}
