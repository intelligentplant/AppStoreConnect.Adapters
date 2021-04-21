using System;
using System.Linq;
using System.Reflection;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterFeaturesCollection"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "String parameter might not always be a URI")]
    public static class AdapterFeaturesCollectionExtensions {

        #region [ Get ]

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// 
        public static TFeature? Get<TFeature>(
            this IAdapterFeaturesCollection features
        ) where TFeature : IAdapterFeature {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }

            var uri = typeof(TFeature).GetAdapterFeatureUri();

            if (uri == null) {
                return default!;
            }

            return features[uri] is TFeature feature ? feature : default!;
        }


        /// <summary>
        /// Gets the specified adapter feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static TFeature? Get<TFeature>(
            this IAdapterFeaturesCollection features, 
            Uri uri
        ) where TFeature : IAdapterFeature {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return features[uri] is TFeature feature ? feature : default!;
        }


        /// <summary>
        /// Gets the specified adapter feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static TFeature? Get<TFeature>(
            this IAdapterFeaturesCollection features, 
            string uriString
        ) where TFeature : IAdapterFeature {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            return features[uri] is TFeature feature 
                ? feature 
                : default!;
        }


        /// <summary>
        /// Gets the specified feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterFeature? Get(this IAdapterFeaturesCollection features, Uri uri) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return features[uri];
        }


        /// <summary>
        /// Gets the specified feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static IAdapterFeature? Get(this IAdapterFeaturesCollection features, string uriString) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            return features[uri];
        }


        /// <summary>
        /// Gets the specified extension feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The extension feature URI. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <returns>
        ///   The extension feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterExtensionFeature? GetExtension(this IAdapterFeaturesCollection features, Uri uri) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!uri.IsAbsoluteUri) {
                // Make URI absolute using WellKnownFeatures.ExtensionFeatureBasePath.
                uri = new Uri(WellKnownFeatures.ExtensionFeatureBasePath, uri);
            }

            uri = uri.EnsurePathHasTrailingSlash();

            return features[uri] is IAdapterExtensionFeature feature
                ? feature
                : default!;
        }


        /// <summary>
        /// Gets the specified extension feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The extension feature URI. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <returns>
        ///   The extension feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static IAdapterExtensionFeature? GetExtension(this IAdapterFeaturesCollection features, string uriString) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            if (!uri.IsAbsoluteUri) {
                // Make URI absolute using WellKnownFeatures.ExtensionFeatureBasePath.
                uri = new Uri(WellKnownFeatures.ExtensionFeatureBasePath, uri);
            }

            uri = uri.EnsurePathHasTrailingSlash();

            return features[uri] is IAdapterExtensionFeature feature
                ? feature
                : default!;
        }

        #endregion

        #region [ TryGet ]

        /// <summary>
        /// Tries to get the specified feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved and is an instance of 
        ///   <typeparamref name="TFeature"/>, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        ///
        public static bool TryGet<TFeature>(
            this IAdapterFeaturesCollection features,
            out TFeature? feature
        ) where TFeature : IAdapterFeature {
            feature = features.Get<TFeature>();
            return feature != null;
        }


        /// <summary>
        /// Tries to get the specified feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved and is an instance of 
        ///   <typeparamref name="TFeature"/>, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGet<TFeature>(
            this IAdapterFeaturesCollection features, 
            Uri uri,
            out TFeature? feature
        ) where TFeature : IAdapterFeature {
            feature = features.Get<TFeature>(uri);
            return feature != null;
        }


        /// <summary>
        /// Tries to get the specified feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved and is an instance of 
        ///   <typeparamref name="TFeature"/>, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static bool TryGet<TFeature>(
            this IAdapterFeaturesCollection features,
            string uriString,
            out TFeature? feature
        ) where TFeature : IAdapterFeature {
            feature = features.Get<TFeature>(uriString);
            return feature != null;
        }


        /// <summary>
        /// Tries to get the specified feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        public static bool TryGet(
            this IAdapterFeaturesCollection features,
            Uri uri,
            out IAdapterFeature? feature
        ) {
            feature = features.Get(uri);
            return feature != null;
        }


        /// <summary>
        /// Tries to get the specified feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static bool TryGet(
            this IAdapterFeaturesCollection features, 
            string uriString, 
            out IAdapterFeature? feature
        ) {
            feature = features.Get(uriString);
            return feature != null;
        }


        /// <summary>
        /// Tries to get the specified extension feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The extension feature URI. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        public static bool TryGetExtension(
            this IAdapterFeaturesCollection features,
            Uri uri,
            out IAdapterExtensionFeature? feature
        ) {
            feature = features.GetExtension(uri);
            return feature != null;
        }


        /// <summary>
        /// Tries to get the specified extension feature implementation.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The extension feature URI string. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static bool TryGetExtension(
            this IAdapterFeaturesCollection features,
            string uriString,
            out IAdapterExtensionFeature? feature
        ) {
            feature = features.GetExtension(uriString);
            return feature != null;
        }

        #endregion

        #region [ Contains ]

        /// <summary>
        /// Checks if the specified adapter feature is defined in the features collection.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The features type.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        public static bool Contains<TFeature>(this IAdapterFeaturesCollection features) where TFeature : IAdapterFeature {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            return features.Keys.Any(x => features[x] is TFeature);
        }


        /// <summary>
        /// Checks if the specified adapter feature is defined in the features collection.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        public static bool Contains(this IAdapterFeaturesCollection features, string uriString) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                return false;
            }

            return features.Keys.Contains(uri);
        }


        /// <summary>
        /// Checks if the specified adapter feature is defined in the features collection.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool Contains(this IAdapterFeaturesCollection features, Uri uri) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return features.Keys.Contains(uri);
        }

        #endregion

    }
}
