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
        public AdapterTypeDescriptor(Uri id, string? name, string? description, string? version, VendorInfo? vendor) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name;
            Description = description;
            if (version == null) {
                Version = null;
            }
            else {
                Version = NuGet.Versioning.SemanticVersion.TryParse(version, out var semVer)
                    ? semVer.ToFullString()
                    : System.Version.TryParse(version, out var v)
                        ? new NuGet.Versioning.SemanticVersion(v.Major, v.Minor, v.Build, string.Empty, v.Revision.ToString(System.Globalization.CultureInfo.CurrentCulture)).ToFullString()
                        : null;
            }
            Vendor = vendor;
        }

    }
}
