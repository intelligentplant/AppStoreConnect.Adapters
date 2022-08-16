using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the vendor for the hosting application.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "URL is for informational use only")]
    public class VendorInfo {

        /// <summary>
        /// The vendor name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The vendor URL.
        /// </summary>
        public string? Url { get; }



        /// <summary>
        /// Creates a new <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="url">
        ///   The vendor URL.
        /// </param>
        public VendorInfo(string? name, string? url) {
            Name = name?.Trim();
            Url = url;
        }


        /// <summary>
        /// Creates a new <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="url">
        ///   The vendor URL.
        /// </param>
        public static VendorInfo Create(string? name, string? url) {
            return new VendorInfo(name, url);
        }


        /// <summary>
        /// Creates a copy of a <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="vendorInfo">
        ///   The object to copy.
        /// </param>
        /// <returns>
        ///   A new <see cref="VendorInfo"/> object, with properties copied from the existing instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="vendorInfo"/> is <see langword="null"/>.
        /// </exception>
        public static VendorInfo FromExisting(VendorInfo vendorInfo) {
            if (vendorInfo == null) {
                throw new ArgumentNullException(nameof(vendorInfo));
            }

            return Create(vendorInfo.Name, vendorInfo.Url);
        }

    }
}
