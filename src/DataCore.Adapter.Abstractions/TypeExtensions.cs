using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

using DataCore.Adapter.Common;
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
            .Where(x => x.IsAnnotatedWithAttributeFeatureAttribute<AdapterFeatureAttribute>())
            .ToArray();


        /// <summary>
        /// Tests if the type is a non-abstract class that is not a generic type definition.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is a non-abstract class that is not a generic 
        ///   type definition, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsConcreteClass(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition;
        }


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

            return 
                // We don't check to see if the type is annotated with [AdapterFeature] when 
                // comparing against standard features, because we use unit tests to ensure 
                // that all standard features are correctly annotated.
                ((type.IsInterface && s_standardAdapterFeatureTypes.Any(f => f.IsAssignableFrom(type))) || (s_adapterExtensionFeatureType.IsAssignableFrom(type) && type.IsAnnotatedWithAttributeFeatureAttribute<AdapterExtensionFeatureAttribute>())) &&
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
        /// Tests if the type is a concrete implementation of an extension adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is a concrete extension adapter feature implementation, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsConcreteExtensionAdapterFeature(this Type type) {
            return type.IsExtensionAdapterFeature() && type.IsConcreteClass();
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
        private static bool IsAnnotatedWithAttributeFeatureAttribute<TAttr>(this Type type) where TAttr : AdapterFeatureAttribute {
            return type.GetAttributeFeatureAttributes<TAttr>().Any();
        }


        /// <summary>
        /// Gets all instances of <see cref="AdapterFeatureAttribute"/> (or a derived type) found 
        /// directly on the type, or on any interfaces the type implements (if the type is a 
        /// non-abstract, non-generic class).
        /// </summary>
        /// <typeparam name="TAttr">
        ///   The attribute type.
        /// </typeparam>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The matching attributes.
        /// </returns>
        private static IEnumerable<(Type type, TAttr attr)> GetAttributeFeatureAttributes<TAttr>(this Type type) where TAttr : AdapterFeatureAttribute {
            var attribute = type.GetCustomAttribute<TAttr>();
            if (attribute != null) {
                yield return (type, attribute);
            }

            if (type.IsConcreteClass()) {
                foreach (var ifType in type.GetInterfaces()) {
                    foreach (var val in ifType.GetAttributeFeatureAttributes<TAttr>()) {
                        yield return val;
                    }
                }
            }
        }


        /// <summary>
        /// Gets the adapter feature interface types implemented by the specified type.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The implemented feature types.
        /// </returns>
        public static IEnumerable<Type> GetAdapterFeatureTypes(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsAdapterFeature()) {
                yield return type;
            }

            foreach (var featureType in type.GetInterfaces().Where(x => x.IsAdapterFeature())) {
                yield return featureType;
            }
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
        /// <remarks>
        ///   If <paramref name="type"/> is a concrete class that implements multiple adapter 
        ///   features, only the first implemented feature URI will be returned.
        /// </remarks>
        public static Uri GetAdapterFeatureUri(this Type type) {
            return type.IsAdapterFeature()
                ? type.GetAttributeFeatureAttributes<AdapterFeatureAttribute>()?.FirstOrDefault().attr?.Uri
                : null;
        }


        /// <summary>
        /// Gets the adapter feature URIs for the type.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The adapter feature URIs for the type.
        /// </returns>
        /// <remarks>
        ///   If <paramref name="type"/> is a concrete class that implements multiple adapter 
        ///   features, the URIs for all implemented features will be returned.
        /// </remarks>
        public static IEnumerable<Uri> GetAdapterFeatureUris(this Type type) {
            return type.IsAdapterFeature()
                ? type.GetAttributeFeatureAttributes<AdapterFeatureAttribute>()?.Select(x => x.attr.Uri)?.ToArray() ?? Array.Empty<Uri>()
                : Array.Empty<Uri>();
        }


        /// <summary>
        /// Tests if the type is annotated with the specified adapter feature URI.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="uriString">
        ///   The adapter feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is annotated with the feature URI, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasAdapterFeatureUri(this Type type, string uriString) {
            if (!UriHelper.TryCreateUriWithTrailingSlash(uriString, out var uri)) {
                return false;
            }
            return type.HasAdapterFeatureUri(uri);
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


        /// <summary>
        /// Tests if a type has an <see cref="AdapterFeatureAttribute"/> that is a child path of 
        /// the specified URI.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="uriString">
        ///   The URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type has an adapter feature URI that is a child path of 
        ///   the specified URI, or <see langword="false"/> otherwise.
        /// </returns>
        private static bool HasAdapterFeatureUriWithPrefix(this Type type, string uriString) {
            if (!UriHelper.TryCreateUriWithTrailingSlash(uriString, out var uri)) {
                return false;
            }

            return type.HasAdapterFeatureUriWithPrefix(uri);
        }


        /// <summary>
        /// Tests if a type has an <see cref="AdapterFeatureAttribute"/> that is a child path of 
        /// the specified URI.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="uri">
        ///   The URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type has an adapter feature URI that is a child path of 
        ///   the specified URI, or <see langword="false"/> otherwise.
        /// </returns>
        private static bool HasAdapterFeatureUriWithPrefix(this Type type, Uri uri) {
            var attr = type.GetCustomAttribute<AdapterFeatureAttribute>();
            if (attr == null || attr.Uri.Equals(uri)) {
                return false;
            }

            var diff = uri.MakeRelativeUri(attr.Uri);
            return !diff.IsAbsoluteUri && !diff.OriginalString.StartsWith("../", StringComparison.Ordinal);
        }


        /// <summary>
        /// Creates a <see cref="FeatureDescriptor"/> from the specified feature type. The type 
        /// must be annotated with <see cref="AdapterFeatureAttribute"/> (or a derived type), or 
        /// it must implement an interface that is annotated in this way.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   A new <see cref="FeatureDescriptor"/> object, or <see langword="null"/> if an 
        ///   <see cref="AdapterFeatureAttribute"/> to create the descriptor from cannot be found.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   If the <paramref name="type"/> (or the interface that it implements) is annotated 
        ///   with a <see cref="DisplayAttribute"/> or a <see cref="DisplayNameAttribute"/>, this 
        ///   will be used to set the <see cref="FeatureDescriptor.DisplayName"/>. If no display 
        ///   name can be inferred, the <see cref="Type.FullName"/> property of the type will be 
        ///   used.
        /// </para>
        /// 
        /// <para>
        ///   If the <paramref name="type"/> (or the interface that it implements) is annotated 
        ///   with a <see cref="DisplayAttribute"/> or a <see cref="DescriptionAttribute"/>, this 
        ///   will be used to set the <see cref="FeatureDescriptor.Description"/>.
        /// </para>
        /// 
        /// </remarks>
        public static FeatureDescriptor CreateFeatureDescriptor(this Type type) {
            if (type == null) {
                return null;
            }

            var attr = type.GetAttributeFeatureAttributes<AdapterFeatureAttribute>().FirstOrDefault();
            if (attr.attr == null) {
                return null;
            }

            var uri = attr.attr.Uri;

            var displayAttr = attr.type.GetCustomAttribute<DisplayAttribute>();
            var displayName = displayAttr?.Name ?? attr.type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            var description = displayAttr?.Description ?? attr.type.GetCustomAttribute<DescriptionAttribute>()?.Description;

            if (string.IsNullOrWhiteSpace(displayName)) {
                displayName = type.FullName;
            }

            if (displayName.Length > FeatureDescriptor.MaxDisplayNameLength) {
                displayName = displayName.Substring(0, FeatureDescriptor.MaxDisplayNameLength);
            }

            if (description != null && description.Length > FeatureDescriptor.MaxDescriptionLength) {
                description = description.Substring(0, FeatureDescriptor.MaxDescriptionLength);
            }

            return new FeatureDescriptor() {
                Uri = uri,
                DisplayName = displayName,
                Description = description
            };
        }


        /// <summary>
        /// Creates a <see cref="FeatureDescriptor"/> from the specified feature type. The type 
        /// must be annotated with <see cref="AdapterFeatureAttribute"/> (or a derived type), or 
        /// it must implement an interface that is annotated in this way.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <returns>
        ///   A new <see cref="FeatureDescriptor"/> object, or <see langword="null"/> if an 
        ///   <see cref="AdapterFeatureAttribute"/> to create the descriptor from cannot be found.
        /// </returns>
        public static FeatureDescriptor CreateFeatureDescriptor<TFeature>(this TFeature feature) where TFeature : IAdapterFeature {
            return typeof(TFeature).CreateFeatureDescriptor();
        }

    }
}
