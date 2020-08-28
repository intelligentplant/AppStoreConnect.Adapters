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
        /// <param name="uriString">
        ///   The absolute feature URI. Well-known URIs are defined in <see cref="WellKnownFeatures"/>. 
        ///   Note that the URI assigned to the <see cref="Uri"/> property will always have a trailing 
        ///   forwards slash (/) appended if required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a valid URI.
        /// </exception>
        public AdapterFeatureAttribute(string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!UriHelper.TryCreateUriWithTrailingSlash(uriString, out var uri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }
            Uri = uri;
        }

    }
}
