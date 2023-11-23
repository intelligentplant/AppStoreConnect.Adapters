using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IRawKeyValueStore"/> that wraps an existing <see cref="IRawKeyValueStore"/> and 
    /// automatically modifies keys passed in or out of the store using a scoped prefix.
    /// </summary>
    internal class ScopedRawKeyValueStore : ScopedKeyValueStore, IRawKeyValueStore {

        /// <summary>
        /// The inner <see cref="IRawKeyValueStore"/>.
        /// </summary>
        internal new IRawKeyValueStore Inner => (IRawKeyValueStore) base.Inner;


        /// <summary>
        /// Creates a new <see cref="ScopedRawKeyValueStore"/> object.
        /// </summary>
        /// <param name="prefix">
        ///   The key prefix for the store.
        /// </param>
        /// <param name="inner">
        ///   The inner <see cref="IRawKeyValueStore"/> to wrap.
        /// </param>
        public ScopedRawKeyValueStore(KVKey prefix, IRawKeyValueStore inner) 
            : base(prefix, inner) { }


        /// <inheritdoc/>
        public ValueTask<byte[]?> ReadRawAsync(KVKey key) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.ReadRawAsync(k);
        }


        /// <inheritdoc/>
        public ValueTask WriteRawAsync(KVKey key, byte[] value) {
            var k = KeyValueStore.AddPrefix(Prefix, key);
            return Inner.WriteRawAsync(k, value);
        }

    }
}
