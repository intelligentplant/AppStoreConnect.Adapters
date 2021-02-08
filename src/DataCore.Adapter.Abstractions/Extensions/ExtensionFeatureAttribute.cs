using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// <see cref="ExtensionFeatureAttribute"/> is used to annotate extension adapter 
    /// features (i.e. interfaces inheriting from <see cref="IAdapterExtensionFeature"/>) to 
    /// provide additional metadata describing the feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExtensionFeatureAttribute : AdapterFeatureAttribute {

        /// <summary>
        /// Creates a new <see cref="ExtensionFeatureAttribute"/>.
        /// </summary>
        /// <param name="uriString">
        ///   The relative feature URI. The absolute feature URI will always be relative to 
        ///   <see cref="WellKnownFeatures.Extensions.BaseUri"/>. Note that the 
        ///   URI assigned to the <see cref="Uri"/> property will always have a trailing 
        ///   forwards slash (/) appended if required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a valid relative URI.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is a relative URI that results in an absolute path that 
        ///   is not a child path of <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="uriString"/> may be specified as an absolute URI if it is a child 
        ///   path of <see cref="WellKnownFeatures.Extensions.BaseUri"/>
        /// </remarks>
        public ExtensionFeatureAttribute(string uriString) 
            : base(WellKnownFeatures.Extensions.BaseUri, uriString) { }

    }

}
