using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IKeyValueStore"/> implementation that uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> 
    /// to store values.
    /// </summary>
    /// <remarks>
    ///   This implementation does not provide any persistence of data.
    /// </remarks>
    public sealed class InMemoryKeyValueStore : IKeyValueStore, IDisposable {

        /// <summary>
        /// The value store.
        /// </summary>
        private readonly ConcurrentDictionary<string, object?> _values = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);


        /// <inheritdoc/>
        public ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(byte[] key, TValue? value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            var keyAsString = Encoding.UTF8.GetString(key);
            _values[keyAsString] = value;
            return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.OK);
        }


        /// <inheritdoc/>
        public ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(byte[] key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var keyAsString = Encoding.UTF8.GetString(key);
            if (!_values.TryGetValue(keyAsString, out var value)) {
                return new ValueTask<KeyValueStoreReadResult<TValue>>(new KeyValueStoreReadResult<TValue>(KeyValueStoreOperationStatus.NotFound, default));
            }

            return new ValueTask<KeyValueStoreReadResult<TValue>>(new KeyValueStoreReadResult<TValue>(KeyValueStoreOperationStatus.OK, (TValue?) value));
        }


        /// <inheritdoc/>
        public ValueTask<KeyValueStoreOperationStatus> DeleteAsync(byte[] key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var keyAsString = Encoding.UTF8.GetString(key);
            if (!_values.TryRemove(keyAsString, out _)) {
                return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.NotFound);
            }

            return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.OK);
        }


        /// <inheritdoc/>
        public IEnumerable<byte[]> GetKeys() {
            foreach (var item in _values.Keys) {
                yield return Encoding.UTF8.GetBytes(item);
            };
        }


        /// <inheritdoc/>
        public void Dispose() {
            _values.Clear();
        }

    }
}
