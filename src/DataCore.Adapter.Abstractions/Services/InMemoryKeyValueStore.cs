using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json;
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
        private readonly ConcurrentDictionary<string, object?> _values = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);


        /// <inheritdoc/>
        protected sealed override CompressionLevel GetCompressionLevel() => CompressionLevel.NoCompression;


        /// <inheritdoc/>
        protected sealed override JsonSerializerOptions? GetJsonSerializerOptions() => null;


        /// <inheritdoc/>
        protected override ValueTask WriteAsync<T>(KVKey key, T value) {
            var keyAsString = System.Text.Encoding.UTF8.GetString(key);
            _values[keyAsString] = value;

            return default;
        }


        /// <inheritdoc/>
        protected override ValueTask<T?> ReadAsync<T>(KVKey key) where T : default {
            var keyAsString = System.Text.Encoding.UTF8.GetString(key);
            if (!_values.TryGetValue(keyAsString, out var value) || value is not T valueActual) {
                return default;
            }

            return new ValueTask<T?>(valueActual);
        }


        /// <inheritdoc/>
        protected override ValueTask<bool> DeleteAsync(KVKey key) {
            var keyAsString = System.Text.Encoding.UTF8.GetString(key);
            return new ValueTask<bool>(_values.TryRemove(keyAsString, out _));
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            await Task.Yield();

            var prefixString = prefix == null || prefix.Value.Length == 0
                ? null
                : System.Text.Encoding.UTF8.GetString(prefix.Value);

            foreach (var key in _values.Keys) {
                if (prefixString != null && !key.StartsWith(prefixString, StringComparison.Ordinal)) {
                    continue;
                }

                yield return key;
            };
        }


        /// <inheritdoc/>
        public void Dispose() {
            _values.Clear();
        }

    }
}
