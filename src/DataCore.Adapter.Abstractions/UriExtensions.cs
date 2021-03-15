using System;
using System.Linq;

namespace DataCore.Adapter {

    /// <summary>
    /// Helper methods for working with <see cref="Uri"/> instances.
    /// </summary>
    public static class UriExtensions {

        /// <summary>
        /// Tests if the URI is a standard adapter feature URI.
        /// </summary>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="uri"/> is a standard adapter feature 
        ///   URI, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsStandardFeatureUri(this Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return WellKnownFeatures.UriCache.Values.Contains(uri) && !uri.Equals(WellKnownFeatures.ExtensionFeatureBasePath);
        }


        /// <summary>
        /// Tests if the URI is an extension adapter feature URI.
        /// </summary>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="uri"/> is a subpath of 
        ///   <see cref="WellKnownFeatures.Extensions.BaseUri"/>, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsExtensionFeatureUri(this Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return uri.IsChildOf(WellKnownFeatures.ExtensionFeatureBasePath);
        }


        /// <summary>
        /// Tests if the URI string is an extension adapter feature URI.
        /// </summary>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="uriString"/> is a subpath of 
        ///   <see cref="WellKnownFeatures.Extensions.BaseUri"/>, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsExtensionFeatureUri(this string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                return false;
            }

            return uri.IsChildOf(WellKnownFeatures.UriCache[WellKnownFeatures.Extensions.BaseUri]);
        }

    }
}
