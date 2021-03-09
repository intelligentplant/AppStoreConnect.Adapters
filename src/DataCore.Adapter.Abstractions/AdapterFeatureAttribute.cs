using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="AdapterFeatureAttribute"/> is used to annotate adapter features (i.e. 
    /// interfaces inheriting from <see cref="IAdapterFeature"/>) to provide additional 
    /// metadata describing the feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class AdapterFeatureAttribute : Attribute {

        /// <summary>
        /// The localised display name.
        /// </summary>
        private readonly LocalizableString _name = new LocalizableString(nameof(Name));

        /// <summary>
        /// The localised description.
        /// </summary>
        private readonly LocalizableString _description = new LocalizableString(nameof(Description));

        /// <summary>
        /// The resource type used to retrieved localised values for the display name and 
        /// description.
        /// </summary>
        private Type? _resourceType;

        /// <summary>
        /// The feature URI. Well-known URIs are defined in <see cref="WellKnownFeatures"/>.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// The type that contains the resources for the <see cref="Name"/> and <see cref="Description"/> properties.
        /// </summary>
        public Type? ResourceType {
            get => _resourceType;
            set {
                if (_resourceType != value) {
                    _resourceType = value;

                    _name.ResourceType = value;
                    _description.ResourceType = value;
                }
            }
        }


        /// <summary>
        /// The display name for the feature.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Following convention used by DisplayAttribute")]
        public string? Name { 
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// The description for the feature.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Following convention used by DisplayAttribute")]
        public string? Description {
            get => _description.Value;
            set => _description.Value = value;
        }


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

            if (!uriString.TryCreateUriWithTrailingSlash(out var uri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }

            Uri = uri!;
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

            if (!baseUriString.TryCreateUriWithTrailingSlash(out var baseUri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(baseUriString));
            }

            if (!Uri.TryCreate(relativeUriString, UriKind.RelativeOrAbsolute, out var relativeUri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(relativeUriString));
            }

            var absoluteUri = (relativeUri.IsAbsoluteUri ? relativeUri : new Uri(baseUri!, relativeUri)).EnsurePathHasTrailingSlash();

            if (!absoluteUri.IsChildOf(baseUri!)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(relativeUriString));
            }

            Uri = absoluteUri;
        }


        /// <summary>
        /// Gets the display name for the adapter feature. This can be either a literal string 
        /// specified by the <see cref="Name"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Name"/> represents a resource 
        /// key within the resource type.
        /// </summary>
        /// <returns>
        ///   The display name for the adapter feature.
        /// </returns>
        public string? GetName() => _name.GetLocalizableValue();


        /// <summary>
        /// Gets the description for the adapter feature. This can be either a literal string 
        /// specified by the <see cref="Description"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Description"/> represents a 
        /// resource key within the resource type.
        /// </summary>
        /// <returns>
        ///   The description for the adapter feature.
        /// </returns>
        public string? GetDescription() => _description.GetLocalizableValue();

    }
}
