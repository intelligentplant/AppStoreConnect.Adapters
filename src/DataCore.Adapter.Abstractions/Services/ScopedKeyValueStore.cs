using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IKeyValueStore"/> that wraps an existing <see cref="IKeyValueStore"/> and 
    /// automatically modifies keys passed in or out of the store using a scoped prefix.
    /// </summary>
    internal class ScopedKeyValueStore : IKeyValueStore {

        /// <summary>
        /// The prefix to apply to keys read from or written to the inner store.
        /// </summary>
        internal KVKey Prefix { get; }

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
        public ScopedKeyValueStore(KVKey prefix, IKeyValueStore inner) {
            if (prefix.Value.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(prefix));
            }
            if (inner == null) {
                throw new ArgumentNullException(nameof(inner));
            }

            if (inner is ScopedKeyValueStore scoped) {
                Prefix = KeyValueStore.AddPrefix(scoped.Prefix, prefix);
                Inner = scoped.Inner;
            }
            else {
                Prefix = prefix;
                Inner = inner;
            }
        }


        /// <inheritdoc/>
        public ValueTask WriteAsync<T>(KVKey key, T value) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.WriteAsync(k, value);
        }


        /// <inheritdoc/>
        public ValueTask<T?> ReadAsync<T>(KVKey key) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.ReadAsync<T>(k);
        }


        /// <inheritdoc/>
        public ValueTask<bool> ExistsAsync(KVKey key) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.ExistsAsync(k);
        }


        /// <inheritdoc/>
        public ValueTask<bool> DeleteAsync(KVKey key) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.DeleteAsync(k);
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            var k = prefix == null 
                ? Prefix
                : KeyValueStore.AddPrefix(Prefix, prefix.Value);
            await foreach (var item in Inner.GetKeysAsync(k).ConfigureAwait(false)) {
                yield return KeyValueStore.RemovePrefix(Prefix, item);
            }
        }

    }
}
