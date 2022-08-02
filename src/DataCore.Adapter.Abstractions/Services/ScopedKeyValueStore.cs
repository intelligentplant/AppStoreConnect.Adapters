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
    ///   <see cref="KeyValueStoreExtensions.CreateScopedStore(IKeyValueStore, KVKey)"/> 
    ///   extension method.
    /// </remarks>
    public class ScopedKeyValueStore : IKeyValueStore {

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
            Prefix = prefix;
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }


        /// <inheritdoc/>
        public ValueTask WriteAsync(KVKey key, byte[] value) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.WriteAsync(k, value);
        }


        /// <inheritdoc/>
        public ValueTask<byte[]?> ReadAsync(KVKey key) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.ReadAsync(k);
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
