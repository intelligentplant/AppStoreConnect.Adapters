using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {
    /// <summary>
    /// Extensions for <see cref="IKeyValueStore"/>.
    /// </summary>
    public static class KeyValueStoreExtensions {

        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="prefix"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, KVKey prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store is KeyValueStore baseStore
                ? baseStore.CreateScopedStore(prefix)
                : new ScopedKeyValueStore(prefix, store);
        }


        /// <summary>
        /// Gets the keys that are defined in the store, converted to <see cref="string"/>.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="prefix">
        ///   Only keys with this prefix will be returned.
        /// </param>
        /// <returns>
        ///   The keys, converted to strings.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Each key is converted to a string by calling <see cref="Encoding.GetString(byte[])"/> 
        ///   on <see cref="Encoding.UTF8"/>. If an exception is thrown during this conversion, the 
        ///   key will be converted to a string using <see cref="BitConverter.ToString(byte[])"/> 
        ///   instead.
        /// </remarks>
        public static async IAsyncEnumerable<string> GetKeysAsStrings(this IKeyValueStore store, KVKey? prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            await foreach (var key in store.GetKeysAsync(prefix).ConfigureAwait(false)) {
                string result;
                try {
                    result = Encoding.UTF8.GetString(key);
                }
                catch {
                    result = BitConverter.ToString(key);
                }
                yield return result;
            }
        }


        /// <summary>
        /// Gets the keys that are defined in the store, converted to <see cref="string"/>.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <returns>
        ///   The keys, converted to strings.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Each key is converted to a string by calling <see cref="Encoding.GetString(byte[])"/> 
        ///   on <see cref="Encoding.UTF8"/>. If an exception is thrown during this conversion, the 
        ///   key will be converted to a string using <see cref="BitConverter.ToString(byte[])"/> 
        ///   instead.
        /// </remarks>
        public static IAsyncEnumerable<string> GetKeysAsStrings(this IKeyValueStore store) {
            return store.GetKeysAsStrings(default);
        }

    }
}
