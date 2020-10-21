using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="AdapterAttribute"/> is used to annotate concrete implementations of <see cref="IAdapter"/> 
    /// to provide metadata about an adapter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AdapterAttribute : Attribute {

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
        public AdapterAttribute(string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!UriExtensions.TryCreateUriWithTrailingSlash(uriString, out var uri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }

            Uri = uri!;
        }


        /// <summary>
        /// Gets the display name for the adapter type. This can be either a literal string 
        /// specified by the <see cref="Name"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Name"/> represents a resource 
        /// key within the resource type.
        /// </summary>
        /// <returns>
        ///   The display name for the adapter type.
        /// </returns>
        public string? GetName() => _name.GetLocalizableValue();


        /// <summary>
        /// Gets the description for the adapter type. This can be either a literal string 
        /// specified by the <see cref="Description"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Description"/> represents a 
        /// resource key within the resource type.
        /// </summary>
        /// <returns>
        ///   The description for the adapter type.
        /// </returns>
        public string? GetDescription() => _description.GetLocalizableValue();

    }
}
