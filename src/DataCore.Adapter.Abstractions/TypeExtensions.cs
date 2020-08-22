using System;
using System.Linq;
using System.Reflection;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="Type"/> instances.
    /// </summary>
    public static class TypeExtensions {

        /// <summary>
        /// <see cref="IAdapterFeature"/> type.
        /// </summary>
        private static readonly Type s_adapterFeatureType = typeof(IAdapterFeature);

        /// <summary>
        /// <see cref="IAdapterExtensionFeature"/> type.
        /// </summary>
        private static readonly Type s_adapterExtensionFeatureType = typeof(IAdapterExtensionFeature);

        /// <summary>
        /// Array of all standard adapter feature types.
        /// </summary>
        private static readonly Type[] s_standardAdapterFeatureTypes = typeof(IAdapterFeature)
            .Assembly
            .GetTypes()
            .Where(x => x.IsInterface)
            .Where(x => s_adapterFeatureType.IsAssignableFrom(x))
            .Where(x => x != s_adapterFeatureType)
            .Where(x => x != s_adapterExtensionFeatureType)
            .Where(x => x.IsAnnotatedWithAttributeFeatureAttribute())
            .ToArray();


        /// <summary>
        /// Gets the <see cref="Type"/> objects that correspond to the standard adapter feature 
        /// types.
        /// </summary>
        /// <returns>
        ///   The adapter feature types.
        /// </returns>
        public static Type[] GetStandardAdapterFeatureTypes() {
            return s_standardAdapterFeatureTypes;
        }


        /// <summary>
        /// Tests if the type is an adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is an adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsAdapterFeature(this Type type) {
            if (type == null) {
                return false;
            }

            return type.IsInterface && 
                (s_standardAdapterFeatureTypes.Any(f => f.IsAssignableFrom(type)) || (s_adapterExtensionFeatureType.IsAssignableFrom(type) && type.IsAnnotatedWithAttributeFeatureAttribute())) &&
                type != s_adapterFeatureType && 
                type != s_adapterExtensionFeatureType;
        }


        /// <summary>
        /// Tests if the type is a standard adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is a standard adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsStandardAdapterFeature(this Type type) {
            return type.IsAdapterFeature() && !s_adapterExtensionFeatureType.IsAssignableFrom(type);
        }


        /// <summary>
        /// Tests if the type is an extension adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is an extension adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsExtensionAdapterFeature(this Type type) {
            return type.IsAdapterFeature() && s_adapterExtensionFeatureType.IsAssignableFrom(type);
        }


        /// <summary>
        /// Tests if a type has been annotated with an <see cref="AdapterFeatureAttribute"/>.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type has been annotated with an <see cref="AdapterFeatureAttribute"/>, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        private static bool IsAnnotatedWithAttributeFeatureAttribute(this Type type) {
            return type.GetCustomAttribute<AdapterFeatureAttribute>() != null;
        }


        /// <summary>
        /// Gets the adapter feature URI for the type.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The adapter feature URI for the type, or <see langword="null"/> if the type is not 
        ///   an adapter feature type.
        /// </returns>
        public static Uri GetAdapterFeatureUri(this Type type) {
            return type.IsAdapterFeature() 
                ? type.GetCustomAttribute<AdapterFeatureAttribute>()?.Uri
                : null;
        }


        /// <summary>
        /// Tests if the type is annotated with the specified adapter feature URI.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="uri">
        ///   The adapter feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is annotated with the feature URI, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static bool HasAdapterFeatureUri(this Type type, string uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return type.GetAdapterFeatureUri()?.Equals(uri) ?? false;
        }


        /// <summary>
        /// Tests if the type is annotated with the specified adapter feature URI.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="uri">
        ///   The adapter feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is annotated with the feature URI, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static bool HasAdapterFeatureUri(this Type type, Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return type.GetAdapterFeatureUri()?.Equals(uri) ?? false;
        }

    }
}
