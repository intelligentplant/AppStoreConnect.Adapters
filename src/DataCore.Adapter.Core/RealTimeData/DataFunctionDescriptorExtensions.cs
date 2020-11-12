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

            if (string.Equals(idOrName, descriptor.Id, StringComparison.Ordinal)) {
                return true;
            }

            if (string.Equals(idOrName, descriptor.Name, StringComparison.Ordinal)) {
                return true;
            }

            return descriptor.Aliases.Any(x => string.Equals(idOrName, x));
        }

    }
}
