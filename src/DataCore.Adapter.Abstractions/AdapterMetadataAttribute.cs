﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="AdapterMetadataAttribute"/> is used to annotate concrete implementations of <see cref="IAdapter"/> 
    /// to provide metadata about an adapter type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AdapterMetadataAttribute : Attribute {

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
        /// The adapter type URI.
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
        /// The display name for the adapter type.
        /// </summary>
        public string? Name {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// The description for the adapter type.
        /// </summary>
        public string? Description {
            get => _description.Value;
            set => _description.Value = value;
        }

        /// <summary>
        /// The help URL.
        /// </summary>
        private string? _helpUrl;

        /// <summary>
        /// The help URL for the adapter type.
        /// </summary>
        /// <remarks>
        ///   If specified, this must be parsable to a URL. If an absolute URL is specified, it 
        ///   must use a scheme of <see cref="Uri.UriSchemeHttps"/> or <see cref="Uri.UriSchemeHttp"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   The property is set using a value that cannot be parsed to an absolute or relative URL, 
        ///   or an absolute URL with a scheme other than <see cref="Uri.UriSchemeHttps"/> or 
        ///   <see cref="Uri.UriSchemeHttp"/> is specified.
        /// </exception>
        public string? HelpUrl {
            get => _helpUrl;
            set {
                if (string.IsNullOrWhiteSpace(value)) {
                    _helpUrl = null;
                    return;
                }
                if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var help)) {
                    throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(value));
                }
                if (help.IsAbsoluteUri && !help.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !help.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_AbsoluteUriWithSpecificSchemeRequired, $"{Uri.UriSchemeHttps}, {Uri.UriSchemeHttp}"), nameof(value));
                }
                _helpUrl = help.ToString();
            }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterMetadataAttribute"/>.
        /// </summary>
        /// <param name="uriString">
        ///   The absolute type URI for the adapter. This is used to identify the adapter type 
        ///   only; it is not required that the URI can be dereferenced. Note that the URI 
        ///   assigned to the <see cref="Uri"/> property will always have a trailing forwards 
        ///   slash (/) appended if required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a valid URI.
        /// </exception>
        public AdapterMetadataAttribute(string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!uriString.TryCreateUriWithTrailingSlash(out var uri)) {
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
