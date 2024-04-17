using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Builder for constructing <see cref="AdapterTypeDescriptor"/> instances.
    /// </summary>
    public sealed class AdapterTypeDescriptorBuilder {

        /// <summary>
        /// The adapter type ID.
        /// </summary>
        private Uri _id = default!;

        /// <summary>
        /// The adapter type name.
        /// </summary>
        private string? _name;

        /// <summary>
        /// The adapter type description.
        /// </summary>
        private string? _description;

        /// <summary>
        /// The SemVer v2 version for the adapter type.
        /// </summary>
        private string? _version;

        /// <summary>
        /// The adapter vendor.
        /// </summary>
        private VendorInfo? _vendor;

        /// <summary>
        /// The help URL for the adapter type.
        /// </summary>
        private Uri? _helpUrl;


        /// <summary>
        /// Creates a new <see cref="AdapterTypeDescriptorBuilder"/> instance.
        /// </summary>
        /// <param name="id">
        ///   The adapter type ID.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is not an absolute URI.
        /// </exception>
        public AdapterTypeDescriptorBuilder(Uri id) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }
            if (!id.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(id), SharedResources.Error_AbsoluteUriRequired);
            }
            _id = id;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterTypeDescriptorBuilder"/> instance using an existing 
        /// <paramref name="descriptor"/> to initialise the builder.
        /// </summary>
        /// <param name="descriptor">
        ///   The existing <see cref="AdapterTypeDescriptor"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterTypeDescriptorBuilder(AdapterTypeDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _id = descriptor.Id;
            _name = descriptor.Name;
            _description = descriptor.Description;
            _version = descriptor.Version;
            _vendor = descriptor.Vendor;
            _helpUrl = descriptor.HelpUrl != null && Uri.TryCreate(descriptor.HelpUrl, UriKind.Absolute, out var url)
                ? url
                : null;
        }


        /// <summary>
        /// Sets the adapter type ID.
        /// </summary>
        /// <param name="id">
        ///   The adapter type ID.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is not an absolute URI.
        /// </exception>
        public AdapterTypeDescriptorBuilder WithId(Uri id) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }
            if (!id.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(id), SharedResources.Error_AbsoluteUriRequired);
            }
            _id = id;
            return this;
        }


        /// <summary>
        /// Sets the adapter type name.
        /// </summary>
        /// <param name="name">
        ///   The adapter type name.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        public AdapterTypeDescriptorBuilder WithName(string? name) {
            _name = name;
            return this;
        }


        /// <summary>
        /// Sets the adapter type description.
        /// </summary>
        /// <param name="description">
        ///   The adapter type description.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        public AdapterTypeDescriptorBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }


        /// <summary>
        /// Sets the version of the adapter type.
        /// </summary>
        /// <param name="version">
        ///   The version. Non-SemVer v2-compatible version numbers will be ignored.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        public AdapterTypeDescriptorBuilder WithVersion(string? version) {
            if (version == null) {
                _version = null;
                return this;
            }

            _version = AdapterTypeDescriptor.GetNormalisedVersion(version);
            return this;
        }


        /// <summary>
        /// Sets the vendor information for the adapter type.
        /// </summary>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        public AdapterTypeDescriptorBuilder WithVendor(VendorInfo? vendor) {
            _vendor = vendor;
            return this;
        }


        /// <summary>
        /// Sets the help URL for the adapter type.
        /// </summary>
        /// <param name="helpUrl">
        ///   The help URL
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   Non-<see langword="null"/> <paramref name="helpUrl"/> values will be ignored if they 
        ///   are not absolute URLs.
        /// </remarks>
        public AdapterTypeDescriptorBuilder WithHelpUrl(Uri? helpUrl) {
            _helpUrl = helpUrl != null && helpUrl.IsAbsoluteUri
                ? helpUrl
                : null;

            return this;
        }


        /// <summary>
        /// Sets the help URL for the adapter type.
        /// </summary>
        /// <param name="helpUrl">
        ///   The help URL
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptorBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   Non-<see langword="null"/> <paramref name="helpUrl"/> values will be ignored if they 
        ///   are not absolute URLs.
        /// </remarks>
        public AdapterTypeDescriptorBuilder WithHelpUrl(string? helpUrl) {
            _helpUrl = helpUrl != null && Uri.TryCreate(helpUrl, UriKind.Absolute, out var url)
                ? url
                : null;

            return this;
        }


        /// <summary>
        /// Builds a new <see cref="AdapterTypeDescriptor"/> instance using the configured 
        /// options.
        /// </summary>
        /// <returns>
        ///   A new <see cref="AdapterTypeDescriptor"/> instance.
        /// </returns>
        public AdapterTypeDescriptor Build() {
            return new AdapterTypeDescriptor(_id, _name, _description, _version, _vendor, _helpUrl?.ToString());
        }

    }
}
