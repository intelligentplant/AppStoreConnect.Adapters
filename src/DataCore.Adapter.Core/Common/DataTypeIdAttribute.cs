using System;
using System.Reflection;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Defines the identifier for a type when the type is encoded in an <see cref="EncodedObject"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class DataTypeIdAttribute : Attribute {

        /// <summary>
        /// The ID for the type.
        /// </summary>
        public Uri DataTypeId { get; }


        /// <summary>
        /// Creates a new <see cref="DataTypeIdAttribute"/>.
        /// </summary>
        /// <param name="uriString">
        ///   The absolute type URI. Note that the URI assigned to the <see cref="DataTypeId"/> 
        ///   property will always have a trailing forwards slash (/) appended if required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a valid URI.
        /// </exception>
        public DataTypeIdAttribute(string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!uriString.TryCreateUriWithTrailingSlash(out var uri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }

            DataTypeId = uri!;
        }

    }
}
