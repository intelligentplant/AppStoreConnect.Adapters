using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IKeyValueStore"/> implementation that uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> 
    /// to store values.
    /// </summary>
    /// <remarks>
    ///   This implementation does not provide any persistence of data.
    /// </remarks>
    public sealed class InMemoryKeyValueStore : KeyValueStore, IDisposable {

        /// <summary>
        /// The value store.
        /// </summary>
        private readonly ConcurrentDictionary<string, byte[]> _values = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);


        /// <summary>
        /// Creats a new <see cref="InMemoryKeyValueStore"/> object.
        /// </summary>
        public InMemoryKeyValueStore() : base(default) { }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreOperationStatus> WriteAsync(KVKey key, byte[] value) {
            var keyAsString = ConvertBytesToHexString(key);
            _values[keyAsString] = value;
            return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.OK);
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreReadResult> ReadAsync(KVKey key) {
            var keyAsString = ConvertBytesToHexString(key);
            if (!_values.TryGetValue(keyAsString, out var value)) {
                return new ValueTask<KeyValueStoreReadResult>(new KeyValueStoreReadResult(KeyValueStoreOperationStatus.NotFound, default));
            }

            return new ValueTask<KeyValueStoreReadResult>(new KeyValueStoreReadResult(KeyValueStoreOperationStatus.OK, value));
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreOperationStatus> DeleteAsync(KVKey key) {
            var keyAsString = ConvertBytesToHexString(key);
            if (!_values.TryRemove(keyAsString, out _)) {
                return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.NotFound);
            }

            return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.OK);
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            await Task.Yield();
            foreach (var item in _values.Keys) {
                var bytes = ConvertHexStringToBytes(item);
                if (prefix != null && prefix.Value.Length > 0 && !StartsWithPrefix(prefix.Value.Value, bytes)) {
                    continue;
                }

                yield return bytes;
            };
        }


        /// <inheritdoc/>
        public void Dispose() {
            _values.Clear();
        }

    }
}
