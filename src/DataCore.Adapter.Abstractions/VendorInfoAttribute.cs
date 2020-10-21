using System;

using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="VendorInfoAttribute"/> is used to provide metadata about an adapter vendor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public class VendorInfoAttribute : Attribute {

        /// <summary>
        /// The vendor name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The vendor's URL.
        /// </summary>
        public Uri? Url { get; }


        /// <summary>
        /// Creates a new <see cref="VendorInfoAttribute"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="urlString">
        ///   The vendor URL.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public VendorInfoAttribute(string name, string? urlString) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Url = urlString != null && Uri.TryCreate(urlString, UriKind.Absolute, out var url) && (Uri.UriSchemeHttps.Equals(url.Scheme, StringComparison.OrdinalIgnoreCase) || Uri.UriSchemeHttp.Equals(url.Scheme, StringComparison.OrdinalIgnoreCase))
                ? url
                : null;
        }


        /// <summary>
        /// Creates a <see cref="VendorInfo"/> object using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="VendorInfo"/> object.
        /// </returns>
        public VendorInfo CreateVendorInfo() {
            return VendorInfo.Create(Name, Url?.ToString());
        }

    }
}
