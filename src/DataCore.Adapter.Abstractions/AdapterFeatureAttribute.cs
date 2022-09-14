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
        /// The localised category name.
        /// </summary>
        private readonly LocalizableString _category = new LocalizableString(nameof(Category));

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
        /// The type that contains the resources for the <see cref="Category"/>, <see cref="Name"/> 
        /// and <see cref="Description"/> properties.
        /// </summary>
        public Type? ResourceType {
            get => _resourceType;
            set {
                if (_resourceType != value) {
                    _resourceType = value;

                    _category.ResourceType = value;
                    _name.ResourceType = value;
                    _description.ResourceType = value;
                }
            }
        }


        /// <summary>
        /// The category for the feature.
        /// </summary>
        /// <remarks>
        ///   If a <see cref="ResourceType"/> is specified and <see cref="Category"/> is <see langword="null"/>, 
        ///   a default category will be inferred from the <see cref="Uri"/>.
        /// </remarks>
        public string? Category {
            get => _category.Value;
            set => _category.Value = value;
        }

        /// <summary>
        /// The display name for the feature.
        /// </summary>
        public string? Name { 
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// The description for the feature.
        /// </summary>
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
        /// Gets the category for the adapter feature. This can be either a literal string 
        /// specified by the <see cref="Category"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Category"/> represents a resource 
        /// key within the resource type.
        /// </summary>
        /// <returns>
        ///   The category for the adapter feature.
        /// </returns>
        public string? GetCategory() {
            if (_category.ResourceType != null && _category.Value == null) {
                // A resource type has been specified but a category has not; we will try and
                // return a default category based on the feature URI.
                var defaultCategory = new LocalizableString(nameof(Category)) { 
                    ResourceType = _category.ResourceType
                };

                if (Uri.IsChildOf(WellKnownFeatures.AssetModel.BaseUri)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_AssetModel);
                }
                else if (Uri.IsChildOf(WellKnownFeatures.Diagnostics.BaseUri)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_Diagnostics);
                }
                else if (Uri.IsChildOf(WellKnownFeatures.Events.BaseUri)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_Events);
                }
                else if (Uri.IsChildOf(WellKnownFeatures.Extensions.BaseUri)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_Extensions);
                }
                else if (Uri == new Uri(WellKnownFeatures.Extensions.CustomFunctions)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_Extensions);
                }
                else if (Uri.IsChildOf(WellKnownFeatures.RealTimeData.BaseUri)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_RealTimeData);
                }
                else if (Uri.IsChildOf(WellKnownFeatures.Tags.BaseUri)) {
                    defaultCategory.Value = nameof(AbstractionsResources.Category_Tags);
                }

                return defaultCategory.GetLocalizableValue();
            }

            return _category.GetLocalizableValue();
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
