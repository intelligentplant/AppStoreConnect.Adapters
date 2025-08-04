using System;
using System.Collections.Concurrent;

namespace DataCore.Adapter {

    /// <summary>
    /// A cache for string instances.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   Use <see cref="StringCache"/> to cache frequently-used strings. This can help to reduce memory usage.
    /// </para>
    /// 
    /// <para>
    ///   <see cref="StringCache"/> uses <see cref="Jaahas.StringCache"/> under the hood. String 
    ///   values are interned using one of the following mechanisms:
    /// </para>
    /// 
    /// <list type="number">
    ///   <item>Using <see cref="Jaahas.StringCache.Native"/> (a wrapper around <see cref="string.Intern(string)"/>).</item>
    ///   <item>Using <see cref="Jaahas.StringCache.Shared"/> (uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> to provide thread-safe lookup of interned strings).</item>
    /// </list>
    /// 
    /// <para>
    ///   The dictionary-based cache can significantly improve lookup performance for frequently-used 
    ///   strings compared to native interning. The interning behaviour is configured using the <see cref="UseNativeInternSwitchName"/> 
    ///   <see cref="AppContext"/> switch. The default behaviour is to use <see cref="Jaahas.StringCache.Shared"/> 
    ///   (i.e. the switch is treated as <see langword="false"/> if it is not defined).
    /// </para>
    /// 
    /// <para>
    ///   When using <see cref="Jaahas.StringCache.Shared"/>, the cache can be cleared using the <see cref="Clear"/> 
    ///   method.
    /// </para>
    ///
    /// </remarks>
    public static class StringCache {

        /// <summary>
        /// The name of the <see cref="AppContext"/> switch that controls whether the cache uses 
        /// native interning.
        /// </summary>
        public const string UseNativeInternSwitchName = "Switch.DataCore.Adapter.StringCache.UseNativeIntern";

        /// <summary>
        /// Specifies whether native interning is enabled.
        /// </summary>
        private static bool? s_useNativeIntern;

        /// <summary>
        /// Specifies whether native interning is enabled.
        /// </summary>
        public static bool NativeInternEnabled => s_useNativeIntern ??= GetNativeInternEnabled();


        /// <summary>
        /// Checks if native interning is enabled.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if native interning is enabled; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool GetNativeInternEnabled() => AppContext.TryGetSwitch(UseNativeInternSwitchName, out var useNativeIntern) && useNativeIntern;


        /// <summary>
        /// The number of strings in the cache.
        /// </summary>
        /// <remarks>
        ///   <see cref="Count"/> will always return -1 if native interning is enabled.
        /// </remarks>
        public static int Count => NativeInternEnabled
            ? Jaahas.StringCache.Native.Count
            : Jaahas.StringCache.Shared.Count;


        /// <summary>
        /// Retrieves the interned reference to the specified string.
        /// </summary>
        /// <param name="str">
        ///   The string to intern.
        /// </param>
        /// <returns>
        ///   The interned string.
        /// </returns>
        public static string? Intern(string? str) {
            return NativeInternEnabled
                ? Jaahas.StringCache.Native.Intern(str!)
                : Jaahas.StringCache.Shared.Intern(str!);
        }


        /// <summary>
        /// Gets a reference to the specified string.
        /// </summary>
        /// <param name="str">
        ///   The string to get a reference to.
        /// </param>
        /// <returns>
        ///   A reference to <paramref name="str"/> if it has been added to the cache; otherwise, 
        ///   <see langword="null"/>.
        /// </returns>
        public static string? Get(string? str) {
            if (str == null) {
                return null;
            }

            return NativeInternEnabled
                ? Jaahas.StringCache.Native.Get(str)
                : Jaahas.StringCache.Shared.Get(str);
        }


        /// <summary>
        /// Calculates the total size of the interned strings in bytes.
        /// </summary>
        /// <returns>
        ///   The total size of the cached strings in bytes. Returns -1 when native interning is enabled.
        /// </returns>
        /// <remarks>
        ///   This method calculates the size based on UTF-16 encoding (2 bytes per character).
        /// </remarks>
        public static long CalculateSize() {
            return NativeInternEnabled
                ? Jaahas.StringCache.Native.CalculateSize()
                : Jaahas.StringCache.Shared.CalculateSize();
        }


        /// <summary>
        /// Clears the cache of interned strings.
        /// </summary>
        /// <remarks>
        ///   Calling this method has no effect if native interning is enabled.
        /// </remarks>
        public static void Clear() {
            if (NativeInternEnabled) {
                return;
            }

            Jaahas.StringCache.Shared.Clear();
        }

    }
}
