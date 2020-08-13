using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the vendor for the hosting application.
    /// </summary>
    public class VendorInfo {

        /// <summary>
        /// The vendor name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The vendor URL.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string Url { get; }
#pragma warning restore CA1056 // Uri properties should not be strings


        /// <summary>
        /// Creates a new <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="url">
        ///   The vendor URL.
        /// </param>
#pragma warning disable CA1054 // Uri parameters should not be strings
        public VendorInfo(string name, string url) {
#pragma warning restore CA1054 // Uri parameters should not be strings
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
#pragma warning disable CA1054 // Uri parameters should not be strings
        public static VendorInfo Create(string name, string url) {
#pragma warning restore CA1054 // Uri parameters should not be strings
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
