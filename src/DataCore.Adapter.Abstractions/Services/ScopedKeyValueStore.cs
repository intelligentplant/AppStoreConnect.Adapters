using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IKeyValueStore"/> that wraps an existing <see cref="IKeyValueStore"/> and 
    /// automatically modifies keys passed in or out of the store using a scoped prefix.
    /// </summary>
    /// <remarks>
    ///   You can simplify creating instances of this class by using the 
    ///   <see cref="KeyValueStoreExtensions.CreateScopedStore(IKeyValueStore, string)"/> 
    ///   extension method.
    /// </remarks>
    public class ScopedKeyValueStore : IKeyValueStore {

        /// <summary>
        /// The key prefix for the store.
        /// </summary>
        internal byte[] Prefix { get; }

        /// <summary>
        /// The inner <see cref="IKeyValueStore"/>.
        /// </summary>
        internal IKeyValueStore Inner { get; }


        /// <summary>
        /// Creates a new <see cref="ScopedKeyValueStore"/> object.
        /// </summary>
        /// <param name="prefix">
        ///   The key prefix for the store.
        /// </param>
        /// <param name="inner">
        ///   The inner <see cref="IKeyValueStore"/> to wrap.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="inner"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="inner"/> is an instance of <see cref="ScopedKeyValueStore"/>, the 
        ///   new instance will concatenate the existing scoped store's key prefix with 
        ///   <paramref name="prefix"/>, and then wrap the same inner <see cref="IKeyValueStore"/> 
        ///   as <paramref name="inner"/>, rather than wrapping <paramref name="inner"/> itself. 
        ///   This ensures that key prefixes do not have to be recursively applied in each 
        ///   operation when a scoped store is created from another scoped store.
        /// </remarks>
        public ScopedKeyValueStore(byte[] prefix, IKeyValueStore inner) {
            if (prefix == null) {
                throw new ArgumentNullException(nameof(prefix));
            }
            if (inner == null) {
                throw new ArgumentNullException(nameof(inner));
            }

            if (inner is ScopedKeyValueStore scopedStore) {
                // The inner store is an instance of ScopedKeyValueStore. Instead of wrapping the
                // scoped store and recursively applying key prefixes in every operation, we will
                // wrap the inner store and concatenate the existing prefix and the prefix
                // specified in the constructor.
                Prefix = AddPrefix(scopedStore.Prefix, prefix);

                Inner = scopedStore.Inner;
            }
            else {
                Prefix = prefix;
                Inner = inner;
            }
        }


        /// <summary>
        /// Combines the specified prefix and key.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix.
        /// </param>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   The combined prefix and key.
        /// </returns>
        private static byte[] AddPrefix(byte[] prefix, byte[] key) {
            if (prefix.Length == 0) {
                return key;
            }

            var result = new byte[prefix.Length + key.Length];
            Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
            Buffer.BlockCopy(key, 0, result, prefix.Length, key.Length);

            return result;
        }


        /// <summary>
        /// Removes a prefix from a key. 
        /// </summary>
        /// <param name="prefix">
        ///   The prefix.
        /// </param>
        /// <param name="key">
        ///   The prefixed key.
        /// </param>
        /// <returns>
        ///   The key without the prefix.
        /// </returns>
        /// <remarks>
        ///   This method assumes that <see cref="StartsWithPrefix"/> has already been called to 
        ///   confirm that <paramref name="key"/> starts with <paramref name="prefix"/>.
        /// </remarks>
        private static byte[] RemovePrefix(byte[] prefix, byte[] key) {
            if (prefix.Length == 0) {
                return key;
            }

            var result = new byte[key.Length - prefix.Length];
            Buffer.BlockCopy(key, prefix.Length, result, 0, result.Length);

            return result;
        }


        /// <summary>
        /// Checks if the specified key starts with a given prefix.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix.
        /// </param>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="key"/> starts with the <paramref name="prefix"/>, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        private static bool StartsWithPrefix(byte[] prefix, byte[] key) {
            if (prefix.Length == 0) {
                return true;
            }
            if (key.Length < prefix.Length) {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++) { 
                if (prefix[i] != key[i]) {
                    return false;
                }
            }

            return true;
        }


        /// <inheritdoc/>
        public async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(byte[] key, TValue? value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return await Inner.WriteAsync(AddPrefix(Prefix, key), value).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(byte[] key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return await Inner.ReadAsync<TValue>(AddPrefix(Prefix, key)).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(byte[] key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return await Inner.DeleteAsync(AddPrefix(Prefix, key));
        }


        /// <inheritdoc/>
        public IEnumerable<byte[]> GetKeys() {
            foreach (var key in Inner.GetKeys()) {
                if (!StartsWithPrefix(Prefix, key)) {
                    // Key does not have the prefix added by this store.
                    continue;
                }

                yield return RemovePrefix(Prefix, key);
            }
        }

    }
}
