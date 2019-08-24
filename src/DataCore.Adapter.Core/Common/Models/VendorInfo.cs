using System;

namespace DataCore.Adapter.Common.Models {

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
        public string Url { get; }


        /// <summary>
        /// Creates a new <see cref="VendorInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The vendor name.
        /// </param>
        /// <param name="url">
        ///   The vendor URL.
        /// </param>
        public VendorInfo(string name, string url) {
            Name = name?.Trim();
            Url = url;
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

            return new VendorInfo(vendorInfo.Name, vendorInfo.Url);
        }

    }
}
