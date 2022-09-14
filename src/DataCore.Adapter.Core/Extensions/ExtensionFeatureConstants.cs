using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Constants related to the deprecation of extension features, for use with <c>[Obsolete]</c> instances.
    /// </summary>
    public static class ExtensionFeatureConstants {

        /// <summary>
        /// Deprecation message.
        /// </summary>
        public const string ObsoleteMessage = "Extension features are deprecated and will be removed in a future release. Use custom functions instead.";

        /// <summary>
        /// Deprecation error.
        /// </summary>
        public const bool ObsoleteError = false;

    }
}
