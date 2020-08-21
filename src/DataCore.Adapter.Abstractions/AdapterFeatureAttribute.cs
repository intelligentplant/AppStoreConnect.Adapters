using System;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="AdapterFeatureAttribute"/> is used to annotate adapter features (i.e. 
    /// interfaces inheriting from <see cref="IAdapterFeature"/>) to provide additional 
    /// metadata describing the feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class AdapterFeatureAttribute : Attribute {

        /// <summary>
        /// The feature URI. Well-known URIs are defined in <see cref="WellKnownFeatures"/>.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// The display name for the feature.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The description for the feature.
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Creates a new <see cref="AdapterFeatureAttribute"/>.
        /// </summary>
        /// <param name="uri">
        ///   The feature URI. Well-known URIs are defined in <see cref="WellKnownFeatures"/>.
        /// </param>
        public AdapterFeatureAttribute(string uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) {
                throw new ArgumentException(Resources.Error_InvalidFeatureUri, nameof(uri));
            }
            Uri = u;
        }

    }
}
