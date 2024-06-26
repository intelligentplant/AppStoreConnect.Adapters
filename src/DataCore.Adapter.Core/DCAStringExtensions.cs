using DataCore.Adapter;

namespace System {

    /// <summary>
    /// Extensions for <see cref="string"/>.
    /// </summary>
    public static class DCAStringExtensions {

        /// <summary>
        /// Interns the string using <see cref="StringCache"/>.
        /// </summary>
        /// <param name="str">
        ///   The string.
        /// </param>
        /// <returns>
        ///   The interned string.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to intern a frequently-used string using <see cref="StringCache"/>. 
        ///   Interning frequently-used strings can reduce memory usage.
        /// </para>
        /// 
        /// <para>
        ///   By default, <see cref="StringCache"/> will intern strings using its own internal 
        ///   caching mechanism. Native interning using <see cref="string.Intern"/> can be enabled 
        ///   by setting the <see cref="StringCache.UseNativeInternSwitchName"/> switch in 
        ///   <see cref="AppContext"/>.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="StringCache"/>
        public static string InternToStringCache(this string str) => StringCache.Intern(str)!;


        /// <summary>
        /// Gets a reference to the specified string from <see cref="StringCache"/>.
        /// </summary>
        /// <param name="str">
        ///   The string.
        /// </param>
        /// <returns>
        ///   A reference to <paramref name="str"/> if it has been added to the <see cref="StringCache"/>; 
        ///   otherwise, <see langword="null"/>.
        /// </returns>
        public static string GetFromStringCache(this string str) => StringCache.Get(str)!;

    }
}
