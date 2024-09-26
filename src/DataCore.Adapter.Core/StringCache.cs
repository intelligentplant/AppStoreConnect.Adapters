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
    ///   <see cref="StringCache"/> can cache strings in two ways:
    /// </para>
    /// 
    /// <list type="number">
    ///   <item>Using <see cref="string.Intern(string)"/>.</item>
    ///   <item>Using an internal <see cref="ConcurrentDictionary{TKey, TValue}"/> to hold the cached string values.</item>
    /// </list>
    /// 
    /// <para>
    ///   The behaviour is configured using the <see cref="UseNativeInternSwitchName"/> <see cref="AppContext"/> 
    ///   switch. The default behaviour is to use an internal cache instead of <see cref="string.Intern(string)"/> 
    ///   (i.e. the switch is treated as <see langword="false"/> if it is not defined).
    /// </para>
    /// 
    /// <para>
    ///   When using an internal dictionary, the cache can be cleared using the <see cref="Clear"/> 
    ///   method.
    /// </para>
    /// 
    /// <para>
    ///   The concept for this cache is taken from https://sergeyteplyakov.github.io/Blog/benchmarking/2023/12/10/Intern_or_Not_Intern.html
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
        /// The internal cache of strings when native interning is disabled.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> s_strings = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

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
            ? -1
            : s_strings.Count;

        /// <summary>
        /// The total size of all cached strings, in bytes.
        /// </summary>
        private static long s_size;

        /// <summary>
        /// The total size of all cached strings, in bytes.
        /// </summary>
        /// <remarks>
        ///   <see cref="Size"/> will always return -1 if native interning is enabled.
        /// </remarks>
        public static long Size => NativeInternEnabled
            ? -1
            : s_size;


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
            if (str == null) {
                return null;
            }

            return NativeInternEnabled
                ? string.Intern(str)
                : s_strings.GetOrAdd(str, OnAddToCache);
        }


        /// <summary>
        /// Updates the recorded size of the cache to include the specified string.
        /// </summary>
        /// <param name="str">
        ///   The string being added to the cache.
        /// </param>
        /// <returns>
        ///   The string being added to the cache.
        /// </returns>
        private static string OnAddToCache(string str) {
            s_size += (str.Length * sizeof(char));
            return str;
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
                ? string.IsInterned(str)
                : s_strings.TryGetValue(str, out var val)
                    ? val
                    : null;
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

            s_strings.Clear();
            s_size = 0;
        }

    }
}
