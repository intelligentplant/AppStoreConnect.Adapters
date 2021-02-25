using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extensions for <see cref="DataFunctionDescriptor"/>.
    /// </summary>
    public static class DataFunctionDescriptorExtensions {

        /// <summary>
        /// Tests if the specified ID or name matches the <see cref="DataFunctionDescriptor.Id"/>, 
        /// <see cref="DataFunctionDescriptor.Name"/> or any of the <see cref="DataFunctionDescriptor.Aliases"/> 
        /// for this function descriptor.
        /// </summary>
        /// <param name="descriptor">
        ///   The data function descriptor.
        /// </param>
        /// <param name="idOrName">
        ///   The data function ID or name to match.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="idOrName"/> matches the descriptor, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsMatch(this DataFunctionDescriptor descriptor, string idOrName) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrWhiteSpace(idOrName)) {
                return false;
            }

            return descriptor.GetIdentifiers().Any(x => string.Equals(idOrName, x, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Tests if the specified descriptor matches the <see cref="DataFunctionDescriptor.Id"/>, 
        /// <see cref="DataFunctionDescriptor.Name"/> or any of the <see cref="DataFunctionDescriptor.Aliases"/> 
        /// for this function descriptor.
        /// </summary>
        /// <param name="descriptor">
        ///   The data function descriptor.
        /// </param>
        /// <param name="other">
        ///   The other descriptor to match against..
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the other descriptor matches this descriptor, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsMatch(this DataFunctionDescriptor descriptor, DataFunctionDescriptor other) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return descriptor.GetIdentifiers().Any(x => other.GetIdentifiers().Contains(x, StringComparer.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Gets all identifiers for the data function descriptor (ID, name, and aliases).
        /// </summary>
        /// <param name="descriptor">
        ///   The data function descriptor.
        /// </param>
        /// <returns>
        ///   A collection of identifiers for the descriptor.
        /// </returns>
        public static IEnumerable<string> GetIdentifiers(this DataFunctionDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            yield return descriptor.Id;
            yield return descriptor.Name;
            foreach (var alias in descriptor.Aliases) {
                yield return alias;
            }
        }

    }
}
