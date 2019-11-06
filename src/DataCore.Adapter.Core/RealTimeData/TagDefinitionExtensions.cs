using System;
using System.Linq;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extensions for <see cref="TagDefinition"/>.
    /// </summary>
    public static class TagDefinitionExtensions {

        /// <summary>
        /// Tests if the tag uses digital states.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag uses digital states, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsDigitalStateTag(this TagDefinition tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            return tag.DataType == Common.VariantType.Int32 && tag.States != null && tag.States.Any();
        }

    }
}
