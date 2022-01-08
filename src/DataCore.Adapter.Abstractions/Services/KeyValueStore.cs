using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Base implementation of <see cref="IKeyValueStore"/>.
    /// </summary>
    /// <remarks>
    ///   Inherit from this class instead of implementing <see cref="IKeyValueStore"/> directly.
    /// </remarks>
    public abstract class KeyValueStore : IKeyValueStore {

        /// <summary>
        /// The prefix to apply to all keys in the store.
        /// </summary>
        private readonly KVKey? _prefix;


        /// <summary>
        /// Creates a new <see cref="KeyValueStore"/> with the specified key prefix.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix to use.
        /// </param>
        protected KeyValueStore(KVKey? prefix) { 
            if (prefix != null && prefix.Value.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(prefix));
            }
            _prefix = prefix;
        }


        /// <summary>
        /// Creates a new <see cref="KeyValueStore"/> with no key prefix.
        /// </summary>
        protected KeyValueStore() : this(null) { }


        async ValueTask<KeyValueStoreOperationStatus> IKeyValueStore.WriteAsync(KVKey key, byte[] value) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            return await WriteAsync(_prefix == null ? key : AddPrefix(_prefix.Value, key), value).ConfigureAwait(false);
        }


        async ValueTask<KeyValueStoreReadResult> IKeyValueStore.ReadAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            return await ReadAsync(_prefix == null ? key : AddPrefix(_prefix.Value, key)).ConfigureAwait(false);
        }


        async ValueTask<KeyValueStoreOperationStatus> IKeyValueStore.DeleteAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            return await DeleteAsync(_prefix == null ? key : AddPrefix(_prefix.Value, key));
        }


        async IAsyncEnumerable<KVKey> IKeyValueStore.GetKeysAsync(KVKey? prefix) {
            if (prefix != null && prefix.Value.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(prefix));
            }

            KVKey? compositePrefix;

            if (_prefix == null) {
                compositePrefix = prefix;
            }
            else {
                compositePrefix = prefix == null
                    ? _prefix
                    : AddPrefix(_prefix.Value, prefix.Value);
            }

            await foreach (var key in GetKeysAsync(compositePrefix)) {
                if (compositePrefix == null) {
                    yield return key;
                }
                else {
                    yield return RemovePrefix(compositePrefix.Value, key);
                }
            }
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <param name="value">
        ///   The value. Guaranteed to be non-null.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the operation result.
        /// </returns>
        protected abstract ValueTask<KeyValueStoreOperationStatus> WriteAsync(KVKey key, byte[] value);


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the operation result.
        /// </returns>
        protected abstract ValueTask<KeyValueStoreReadResult> ReadAsync(KVKey key);


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the operation result.
        /// </returns>
        protected abstract ValueTask<KeyValueStoreOperationStatus> DeleteAsync(KVKey key);


        /// <summary>
        /// Gets all keys from the store that match the specified prefix.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix to match. Can be <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   The matching keys.
        /// </returns>
        protected abstract IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix);


        /// <summary>
        /// Creates a new <see cref="IKeyValueStore"/> from the current store that applies the a 
        /// prefix on all operations.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix to apply.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/> that wraps the current store.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="prefix"/> has a <see cref="KVKey.Value"/> that is <see langword="null"/> 
        ///   or zero-length.
        /// </exception>
        public IKeyValueStore CreateScopedStore(KVKey prefix) {
            if (prefix.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(prefix));
            }

            if (this is ScopedKeyValueStore scoped) {
                // This store is already an instance of ScopedKeyValueStore. Instead of wrapping
                // the scoped store and recursively applying key prefixes in every operation, we
                // will wrap the inner store and concatenate the prefix for this store with the
                // prefix passed to this method.
                return new ScopedKeyValueStore(_prefix == null ? prefix : AddPrefix(_prefix.Value, prefix), scoped.Inner);
            }

            return new ScopedKeyValueStore(prefix, this);
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
        public static KVKey AddPrefix(KVKey prefix, KVKey key) {
            if (prefix.Length == 0) {
                return key;
            }

            if (key.Length == 0) {
                return prefix;
            }

            var result = new byte[prefix.Length + key.Length];
            Buffer.BlockCopy(prefix.Value, 0, result, 0, prefix.Length);
            Buffer.BlockCopy(key.Value, 0, result, prefix.Length, key.Length);

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
        public static KVKey RemovePrefix(KVKey prefix, KVKey key) {
            if (key.Length == 0) {
                return key;
            }

            if (prefix.Length == 0 || !StartsWithPrefix(prefix, key)) {
                return key;
            }

            var result = new byte[key.Length - prefix.Length];
            Buffer.BlockCopy(key.Value, prefix.Length, result, 0, result.Length);

            return result;
        }


        /// <summary>
        /// Checks if the specified key starts with the specified prefix.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix.
        /// </param>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="key"/> starts with the store's prefix, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool StartsWithPrefix(KVKey prefix, KVKey key) {
            if (prefix.Length == 0) {
                return true;
            }

            if (key.Length < prefix.Length) {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++) {
                if (prefix.Value[i] != key.Value[i]) {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">
        ///   The bytes to convert.
        /// </param>
        /// <returns>
        ///   The hex string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="bytes"/> is <see langword="null"/>.
        /// </exception>
        protected string ConvertBytesToHexString(byte[] bytes) {
            if (bytes == null) {
                throw new ArgumentNullException(nameof(bytes));
            }
            return BitConverter.ToString(bytes).Replace("-", "");
        }


        /// <summary>
        /// Converts a hex string to an array of bytes.
        /// </summary>
        /// <param name="hexString">
        ///   The hex string.
        /// </param>
        /// <returns>
        ///   The equivalent byte array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hexString"/> is <see langword="null"/>.
        /// </exception>
        protected byte[] ConvertHexStringToBytes(string hexString) {
            if (hexString == null) {
                throw new ArgumentNullException(nameof(hexString));
            }

            // See https://stackoverflow.com/a/311179
            var outputLength = hexString.Length / 2;
            var bytes = new byte[outputLength];
            for (var i = 0; i < outputLength; i++) {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

    }
}
