using System;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="AdapterFeatureAttribute"/> is used to annotate adapter features (i.e. 
    /// interfaces inheriting from <see cref="IAdapterFeature"/>) to provide additional 
    /// metadata describing the feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
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


        /// <summary>
        /// Creates a new <see cref="AdapterFeatureAttribute"/> with a URI that is relative to the 
        /// specified absolute base path.
        /// </summary>
        /// <param name="baseUriString">
        ///   The absolute base URI. The feature URI will be relative to this path.
        /// </param>
        /// <param name="relativeUriString">
        ///   The relative feature URI. Note that the URI assigned to the <see cref="Uri"/> 
        ///   property will always have a trailing forwards slash (/) appended if required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="baseUriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="relativeUriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="baseUriString"/> is not a valid absolute URI.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="relativeUriString"/> is not a valid relative URI.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="relativeUriString"/> is a relative URI that results in an absolute 
        ///   path that is not a child path of <paramref name="baseUriString"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="relativeUriString"/> can specify an absolute URI if it is a child 
        ///   path of <paramref name="baseUriString"/>.
        /// </remarks>
        internal AdapterFeatureAttribute(string baseUriString, string relativeUriString) {
            if (baseUriString == null) {
                throw new ArgumentNullException(nameof(baseUriString));
            }
            if (relativeUriString == null) {
                throw new ArgumentNullException(nameof(relativeUriString));
            }

            if (!UriHelper.TryCreateUriWithTrailingSlash(baseUriString, out var baseUri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(baseUriString));
            }

            if (!Uri.TryCreate(relativeUriString, UriKind.RelativeOrAbsolute, out var relativeUri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(relativeUriString));
            }

            var absoluteUri = UriHelper.EnsurePathHasTrailingSlash(
                relativeUri.IsAbsoluteUri
                    ? relativeUri
                    : new Uri(baseUri, relativeUri)
            );

            if (!UriHelper.IsChildPath(absoluteUri, baseUri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(relativeUriString));
            }

            Uri = absoluteUri;
        }

    }
}
