using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// <see cref="IKeyValueStore"/> implementation that uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> 
    /// to store values.
    /// </summary>
    /// <remarks>
    ///   This implementation does not provide any persistence of data.
    /// </remarks>
    public sealed class InMemoryKeyValueStore : KeyValueStore<KeyValueStoreOptions>, IDisposable {

        /// <summary>
        /// The value store.
        /// </summary>
        private readonly ConcurrentDictionary<string, byte[]> _values = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);


        /// <summary>
        /// Creats a new <see cref="InMemoryKeyValueStore"/> object.
        /// </summary>
        public InMemoryKeyValueStore(CompressionLevel compressionLevel = CompressionLevel.NoCompression) 
            : base(new KeyValueStoreOptions() { CompressionLevel = compressionLevel }, null) { }


        /// <inheritdoc/>
        protected override ValueTask WriteAsync(KVKey key, byte[] value) {
            var keyAsString = System.Text.Encoding.UTF8.GetString(key);
            _values[keyAsString] = value;
            return default;
        }


        /// <inheritdoc/>
        protected override ValueTask<byte[]?> ReadAsync(KVKey key) {
            var keyAsString = System.Text.Encoding.UTF8.GetString(key);
            if (!_values.TryGetValue(keyAsString, out var value)) {
                return new ValueTask<byte[]?>((byte[]?) null);
            }

            return new ValueTask<byte[]?>(value);
        }


        /// <inheritdoc/>
        protected override ValueTask<bool> DeleteAsync(KVKey key) {
            var keyAsString = System.Text.Encoding.UTF8.GetString(key);
            return new ValueTask<bool>(_values.TryRemove(keyAsString, out _));
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            await Task.Yield();

            var utf8Prefix = prefix == null || prefix.Value.Length == 0
                ? null
                : System.Text.Encoding.UTF8.GetString(prefix.Value);

            foreach (var item in _values.Keys) {
                if (utf8Prefix != null && !item.StartsWith(utf8Prefix, StringComparison.Ordinal)) {
                    continue;
                }

                yield return System.Text.Encoding.UTF8.GetBytes(item);
            };
        }


        /// <inheritdoc/>
        public void Dispose() {
            _values.Clear();
        }

    }
}
