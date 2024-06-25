using System;
using System.Collections.Generic;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Builder for constructing <see cref="HostInfo"/> instances.
    /// </summary>
    public sealed class HostInfoBuilder : AdapterEntityBuilder<HostInfo> {

        /// <summary>
        /// The application name.
        /// </summary>
        private string? _name;

        /// <summary>
        /// The application description.
        /// </summary>
        private string? _description;

        /// <summary>
        /// The application version.
        /// </summary>
        private string? _version;

        /// <summary>
        /// The application vendor information.
        /// </summary>
        private VendorInfo? _vendor;


        /// <summary>
        /// Creates a new <see cref="HostInfoBuilder"/> instance.
        /// </summary>
        public HostInfoBuilder() { }


        /// <summary>
        /// Creates a new <see cref="HostInfoBuilder"/> instance using an existing <see cref="HostInfo"/> 
        /// to initialise the builder.
        /// </summary>
        /// <param name="hostInfo">
        ///   The existing <see cref="HostInfo"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hostInfo"/> is <see langword="null"/>.
        /// </exception>
        public HostInfoBuilder(HostInfo hostInfo) {
            if (hostInfo == null) {
                throw new ArgumentNullException(nameof(hostInfo));
            }

            WithName(hostInfo.Name);
            WithDescription(hostInfo.Description);
            WithVersion(hostInfo.Version);
            WithVendor(hostInfo.Vendor);
            this.WithProperties(hostInfo.Properties);
        }


        /// <summary>
        /// Sets the host application display name.
        /// </summary>
        /// <param name="name">
        ///   The name.
        /// </param>
        /// <returns>
        ///   The <see cref="HostInfoBuilder"/>.
        /// </returns>
        public HostInfoBuilder WithName(string? name) {
            _name = name;
            return this;
        }


        /// <summary>
        /// Sets the host application description.
        /// </summary>
        /// <param name="description">
        ///   The description.
        /// </param>
        /// <returns>
        ///   The <see cref="HostInfoBuilder"/>.
        /// </returns>
        public HostInfoBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }


        /// <summary>
        /// Sets the host application version.
        /// </summary>
        /// <param name="version">
        ///   The version. Non-SemVer v2-compatible version numbers will be ignored.
        /// </param>
        /// <returns>
        ///   The <see cref="HostInfoBuilder"/>.
        /// </returns>
        public HostInfoBuilder WithVersion(string? version) {
            _version = AdapterTypeDescriptor.GetNormalisedVersion(version);
            return this;
        }


        /// <summary>
        /// Sets the vendor information for the host application.
        /// </summary>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <returns>
        ///   The <see cref="HostInfoBuilder"/>.
        /// </returns>
        public HostInfoBuilder WithVendor(VendorInfo? vendor) {
            _vendor = vendor;
            return this;
        }


        /// <summary>
        /// Builds a new <see cref="HostInfo"/> instance using the configured options.
        /// </summary>
        /// <returns>
        ///   A new <see cref="HostInfo"/> instance.
        /// </returns>
        public override HostInfo Build() => new HostInfo(_name, _description, _version, _vendor, GetProperties());

    }
}
