using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Annotates a type with a data type ID that is relative to the base URI for an extension 
    /// feature.
    /// </summary>
    /// <remarks>
    ///   Use this attribute in place of <see cref="Common.DataTypeIdAttribute"/> to simplify type 
    ///   ID registration for input and output types used by an extension feature. The relative URI 
    ///   specified in the constructor is made absolute using the URI of the feature type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ExtensionFeatureDataTypeAttribute : Common.DataTypeIdAttribute {

        /// <summary>
        /// Path for data type URIs relative to the feature URI.
        /// </summary>
        private const string TypesRelativePath = "types/";


        /// <summary>
        /// Creates a new <see cref="ExtensionFeatureDataTypeAttribute"/> object.
        /// </summary>
        /// <param name="featureType">
        ///   The feature type that this type ID is relative to.
        /// </param>
        /// <param name="relativeTypeId">
        ///   The relative type ID.
        /// </param>
        public ExtensionFeatureDataTypeAttribute(Type featureType, string relativeTypeId) 
            : base(new Uri(GetBaseUri(featureType), relativeTypeId).ToString()) { }


        /// <summary>
        /// Gets the base URI for the specified feature type.
        /// </summary>
        /// <param name="type">
        ///   The feature type.
        /// </param>
        /// <returns>
        ///   The feature type URI, or <see langword="null"/> if the feature URI cannot be found.
        /// </returns>
        private static Uri? GetBaseUri(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsExtensionAdapterFeature()) {
                return null;
            }

            var uri = type.GetAdapterFeatureUri();
            if (uri == null) {
                return null;
            }

            return new Uri(uri, TypesRelativePath);
        }

    }

}
