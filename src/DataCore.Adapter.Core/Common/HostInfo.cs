using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the hosting application.
    /// </summary>
    public class HostInfo {

        /// <summary>
        /// The application name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The application description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The Semantic Versioning v2 (https://semver.org/spec/v2.0.0.html) application version.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Information about the application vendor.
        /// </summary>
        public VendorInfo Vendor { get; }

        /// <summary>
        /// Custom properties supplied by the hosting application.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// The <see cref="HostInfo"/> to use when an instance is not provided by the hosting 
        /// application.
        /// </summary>
        public static HostInfo Unspecified { get; } = Create(SharedResources.HostInfo_Unspecified_Name, SharedResources.HostInfo_Unspecified_Description, "0.0.0", null, null);


        /// <summary>
        /// Creates a new <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The host name.
        /// </param>
        /// <param name="description">
        ///   The host description.
        /// </param>
        /// <param name="version">
        ///   The host version. This will be parsed and converted to a Semantic Versioning v2 version 
        ///   (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <param name="properties">
        ///   Additional host properties.
        /// </param>
        public HostInfo(string name, string description, string version, VendorInfo vendor, IEnumerable<AdapterProperty> properties) {
            Name = name?.Trim();
            Description = description?.Trim();
            Version = NuGet.Versioning.SemanticVersion.TryParse(version, out var semVer)
            ? semVer.ToFullString()
            : System.Version.TryParse(version, out var v)
                ? new NuGet.Versioning.SemanticVersion(v.Major, v.Minor, v.Build, string.Empty, v.Revision.ToString()).ToFullString()
                : new NuGet.Versioning.SemanticVersion(0, 0, 0).ToFullString();
            Vendor = vendor;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The host name.
        /// </param>
        /// <param name="description">
        ///   The host description.
        /// </param>
        /// <param name="version">
        ///   The host version. This will be parsed and converted to a Semantic Versioning v2 version 
        ///   (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <param name="properties">
        ///   Additional host properties.
        /// </param>
        public static HostInfo Create(string name, string description, string version, VendorInfo vendor, params AdapterProperty[] properties) {
            return Create(name, description, version, vendor, (IEnumerable<AdapterProperty>) properties);
        }


        /// <summary>
        /// Creates a new <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The host name.
        /// </param>
        /// <param name="description">
        ///   The host description.
        /// </param>
        /// <param name="version">
        ///   The host version. This will be parsed and converted to a Semantic Versioning v2 version 
        ///   (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <param name="properties">
        ///   Additional host properties.
        /// </param>
        public static HostInfo Create(string name, string description, string version, VendorInfo vendor, IEnumerable<AdapterProperty> properties) {
            return new HostInfo(name, description, version, vendor, properties);
        }


        /// <summary>
        /// Creates a copy of a <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The object to copy.
        /// </param>
        /// <returns>
        ///   A new <see cref="HostInfo"/> object, with properties copied from the existing instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hostInfo"/> is <see langword="null"/>.
        /// </exception>
        public static HostInfo FromExisting(HostInfo hostInfo) {
            if (hostInfo == null) {
                throw new ArgumentNullException(nameof(hostInfo));
            }

            return Create(
                hostInfo.Name,
                hostInfo.Description,
                hostInfo.Version,
                hostInfo.Vendor == null
                    ? null
                    : VendorInfo.FromExisting(hostInfo.Vendor),
                hostInfo.Properties
            ); ;
        }

    }
}
