using System;
using System.Collections.Generic;
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
        /// Array of all standard adapter feature type interfaces.
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
        /// type interfaces.
        /// </summary>
        /// <returns>
        ///   The adapter feature types.
        /// </returns>
        public static Type[] GetStandardAdapterFeatureTypes() {
            return s_standardAdapterFeatureTypes;
        }


        /// <summary>
        /// Tests if the type is an adapter feature. See the remarks section for details on how 
        /// adapter features are identified.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is an adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// Adapter feature types are identified in one of the following ways:
        /// 
        /// <list type="bullet">
        /// <item>
        ///   <description>
        ///     The <paramref name="type"/> is an interface that is derived from one of the 
        ///     standard adapter feature interfaces returned by a call to <see cref="GetStandardAdapterFeatureTypes"/>.
        ///   </description>
        /// </item>
        /// <item>
        ///   <description>
        ///     The <paramref name="type"/> is derived from <see cref="IAdapterExtensionFeature"/> 
        ///     and is annotated with an <see cref="ExtensionFeatureAttribute"/>.
        ///   </description>
        /// </item>
        /// </list>
        /// 
        /// </remarks>
        public static bool IsAdapterFeature(this Type type) {
            if (type == null) {
                return false;
            }

            return
                // We don't check to see if the type is annotated with [AdapterFeature] when 
                // comparing against standard features, because we use unit tests to ensure 
                // that all standard features are correctly annotated.
                ((type.IsInterface && s_standardAdapterFeatureTypes.Any(f => f.IsAssignableFrom(type))) || (s_adapterExtensionFeatureType.IsAssignableFrom(type) && type.IsAnnotatedWithAttributeFeatureAttribute<ExtensionFeatureAttribute>())) &&
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
            return type.GetAdapterFeatureAttributes<TAttr>().Any();
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
        private static IEnumerable<(Type type, TAttr attr)> GetAdapterFeatureAttributes<TAttr>(this Type type) where TAttr : AdapterFeatureAttribute {
            var attribute = type.GetCustomAttribute<TAttr>();
            if (attribute != null) {
                yield return (type, attribute);
            }

            if (type.IsConcreteClass()) {
                foreach (var ifType in type.GetInterfaces()) {
                    foreach (var val in ifType.GetAdapterFeatureAttributes<TAttr>()) {
                        yield return val;
                    }
                }
            }
        }


        /// <summary>
        /// Gets the adapter feature types implemented by the specified type.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="type"/> is a concrete class that implements multiple adapter 
        ///   features, only the first implemented feature URI will be returned.
        /// </remarks>
        public static Uri? GetAdapterFeatureUri(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsAdapterFeature()
                ? type.GetAdapterFeatureAttributes<AdapterFeatureAttribute>()?.FirstOrDefault().attr?.Uri
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static IEnumerable<Uri> GetAdapterFeatureUris(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsAdapterFeature()
                ? type.GetAdapterFeatureAttributes<AdapterFeatureAttribute>()
                    ?.Select(x => x.attr.Uri)
                    ?.ToArray() ?? Array.Empty<Uri>()
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
            if (!uriString.TryCreateUriWithTrailingSlash(out var uri)) {
                return false;
            }
            return type.HasAdapterFeatureUri(uri!);
        }


        /// <summary>
        /// Tests if the type is directly annotated with the specified adapter feature URI.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// If the <see cref="AdapterFeatureAttribute"/> does not define a display name, the 
        /// display name will be set to the <see cref="Type.FullName"/> of the <paramref name="type"/>.
        /// </remarks>
        public static FeatureDescriptor? CreateFeatureDescriptor(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            var attr = type.GetAdapterFeatureAttributes<AdapterFeatureAttribute>().FirstOrDefault();

            if (attr.attr == null) {
                return null;
            }

            var uri = attr.attr.Uri;

            var displayName = attr.attr.GetName();
            var description = attr.attr.GetDescription();
            var category = attr.attr.GetCategory();

            if (string.IsNullOrWhiteSpace(displayName)) {
                displayName = type.FullName;
            }

            if (displayName!.Length > FeatureDescriptor.MaxDisplayNameLength) {
                displayName = displayName.Substring(0, FeatureDescriptor.MaxDisplayNameLength);
            }

            if (description != null && description.Length > FeatureDescriptor.MaxDescriptionLength) {
                description = description.Substring(0, FeatureDescriptor.MaxDescriptionLength);
            }

            if (category != null && category.Length > FeatureDescriptor.MaxCategoryLength) {
                category = category.Substring(0, FeatureDescriptor.MaxCategoryLength);
            }

            return new FeatureDescriptor() {
                Uri = uri,
                Category = category,
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
        /// <param name="feature">
        ///   The feature type.
        /// </param>
        /// <returns>
        ///   A new <see cref="FeatureDescriptor"/> object, or <see langword="null"/> if an 
        ///   <see cref="AdapterFeatureAttribute"/> to create the descriptor from cannot be found.
        /// </returns>
        public static FeatureDescriptor? CreateFeatureDescriptor<TFeature>(this TFeature feature) where TFeature : IAdapterFeature {
            return typeof(TFeature).CreateFeatureDescriptor();
        }


        /// <summary>
        /// Creates an <see cref="AdapterTypeDescriptor"/> from the specified adapter type.
        /// </summary>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptor"/> for the adapter type, or <see langword="null"/> 
        ///   if the <paramref name="type"/> is not a concrete class that implements <see cref="IAdapter"/>.
        /// </returns>
        public static AdapterTypeDescriptor? CreateAdapterTypeDescriptor(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(IAdapter).IsAssignableFrom(type)) {
                // Not an adapter type.
                return null;
            }

            if (!type.IsClass || type.IsAbstract) {
                // Not a concrete adapter type.
                return null;
            }
            
            var adapterAttribute = type.GetCustomAttribute<AdapterMetadataAttribute>();
            var vendorAttribute = type.GetCustomAttribute<VendorInfoAttribute>() ?? type.Assembly.GetCustomAttribute<VendorInfoAttribute>();
            var companyName = type.Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

            var uri = adapterAttribute?.Uri ?? new Uri(string.Concat("asc:adapter-type/", type.FullName, "/"));
            return new AdapterTypeDescriptor(
                uri,
                adapterAttribute?.GetName(),
                adapterAttribute?.GetDescription(),
                type.Assembly.GetInformationalVersion(),
                vendorAttribute?.CreateVendorInfo() ?? (string.IsNullOrWhiteSpace(companyName) ? null : new VendorInfo(companyName, null)),
                adapterAttribute?.HelpUrl
            );
        }

    }
}
