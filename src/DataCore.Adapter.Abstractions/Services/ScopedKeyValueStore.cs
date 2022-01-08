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
    public class ScopedKeyValueStore : KeyValueStore {

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
        public ScopedKeyValueStore(KVKey prefix, IKeyValueStore inner) : base(prefix) {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreOperationStatus> WriteAsync(KVKey key, byte[] value) {
            return Inner.WriteAsync(key, value);
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreReadResult> ReadAsync(KVKey key) {
            return Inner.ReadAsync(key);
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreOperationStatus> DeleteAsync(KVKey key) {
            return Inner.DeleteAsync(key);
        }


        /// <inheritdoc/>
        protected override IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            return Inner.GetKeysAsync(prefix);
        }

    }
}
