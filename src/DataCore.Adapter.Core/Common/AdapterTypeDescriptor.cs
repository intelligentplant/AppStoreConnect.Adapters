using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an adapter type.
    /// </summary>
    public class AdapterTypeDescriptor {

        /// <summary>
        /// The URI for the adapter type.
        /// </summary>
        public Uri Id { get; }

        /// <summary>
        /// The display name for the adapter type.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The description for the adapter type.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The adapter type version number, expressed as a Semantic Versioning v2 version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// The vendor information for the adapter type.
        /// </summary>
        public VendorInfo? Vendor { get; }

        /// <summary>
        /// The help URL for the adapter type.
        /// </summary>
        public string? HelpUrl { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterTypeDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The URI for the adapter type.
        /// </param>
        /// <param name="name">
        ///   The adapter type display name.
        /// </param>
        /// <param name="description">
        ///   The adapter type description.
        /// </param>
        /// <param name="version">
        ///   The adapter type version. This will be parsed and converted to a Semantic Versioning 
        ///   v2 version (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The adapter type vendor information.
        /// </param>
        /// <param name="helpUrl">
        ///   The help URL for the adapter type.
        /// </param>
        public AdapterTypeDescriptor(Uri id, string? name, string? description, string? version, VendorInfo? vendor, string? helpUrl) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name;
            Description = description;
            Version = GetNormalisedVersion(version);
            Vendor = vendor;
            HelpUrl = helpUrl;
        }


        /// <summary>
        /// Converts the specified version string into a normalised format.
        /// </summary>
        /// <param name="version">
        ///   The version string.
        /// </param>
        /// <param name="style">
        ///   The <see cref="Semver.SemVersionStyles"/> to use when converting <paramref name="version"/> 
        ///   into a <see cref="Semver.SemVersion"/> instance.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        ///   <see cref="GetNormalisedVersion"/> will attempt to convert <paramref name="version"/> 
        ///   into a <see cref="Semver.SemVersion"/> instance and return a <see cref="string"/> 
        ///   representation of the version.
        /// </remarks>
        internal static string? GetNormalisedVersion(string? version, Semver.SemVersionStyles style = Semver.SemVersionStyles.Strict) {
            if (version == null) {
                return null;
            }

            return Semver.SemVersion.TryParse(version, style, out var semVer)
                ? semVer.ToString()
                : System.Version.TryParse(version, out var v)
                    ? Semver.SemVersion.FromVersion(v).ToString()
                    : null;
        }

    }
}
