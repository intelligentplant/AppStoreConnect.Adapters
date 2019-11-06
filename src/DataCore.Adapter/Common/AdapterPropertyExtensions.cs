using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extensions for <see cref="AdapterProperty"/>.
    /// </summary>
    public static class AdapterPropertyExtensions {

        /// <summary>
        /// Finds the specified named property.
        /// </summary>
        /// <param name="properties">
        ///   The properties to search.
        /// </param>
        /// <param name="name">
        ///   The property name to look for.
        /// </param>
        /// <returns>
        ///   The first entry in <paramref name="properties"/> that matches the specified name. A 
        ///   case-insensitive comparison is used.
        /// </returns>
        public static AdapterProperty FindProperty(this IEnumerable<AdapterProperty> properties, string name) {
            if (properties == null || name == null) {
                return null;
            }

            return properties.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

    }
}
