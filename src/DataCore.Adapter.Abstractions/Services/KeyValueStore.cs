using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Base implementation of <see cref="IKeyValueStore"/>.
    /// </summary>
    /// <remarks>
    ///   Inherit from this class instead of implementing <see cref="IKeyValueStore"/> directly.
    /// </remarks>
    public abstract class KeyValueStore : IKeyValueStore {

        /// <summary>
        /// The logger for the store.
        /// </summary>
        protected ILogger Logger { get; }


        /// <summary>
        /// Creates a new <see cref="KeyValueStore"/>.
        /// </summary>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        protected KeyValueStore(ILogger? logger = null) {
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }


        async ValueTask IKeyValueStore.WriteAsync(KVKey key, byte[] value) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            var compressionLevel = GetCompressionLevel();

            await WriteAsync(
                key, 
                value.Length == 0 || compressionLevel == CompressionLevel.NoCompression
                    ? value 
                    : await CompressDataAsync(value, compressionLevel).ConfigureAwait(false)
                ).ConfigureAwait(false);
        }


        async ValueTask<byte[]?> IKeyValueStore.ReadAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }

            var compressionLevel = GetCompressionLevel();

            var result = await ReadAsync(key).ConfigureAwait(false);
            if (result == null || result.Length == 0 || compressionLevel == CompressionLevel.NoCompression) {
                return result;
            }

            return await DecompressDataAsync(result).ConfigureAwait(false);
        }


        async ValueTask<bool> IKeyValueStore.DeleteAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }

            return await DeleteAsync(key).ConfigureAwait(false);
        }


        IAsyncEnumerable<KVKey> IKeyValueStore.GetKeysAsync(KVKey? prefix) {
            if (prefix != null && prefix.Value.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(prefix));
            }

            return GetKeysAsync(prefix);
        }


        /// <summary>
        /// Gets the compression level to use for store data.
        /// </summary>
        /// <returns>
        /// The compression level.
        /// </returns>
        protected abstract CompressionLevel GetCompressionLevel();


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
        ///   A <see cref="ValueTask"/> that will perform the operation.
        /// </returns>
        protected abstract ValueTask WriteAsync(KVKey key, byte[] value);


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the value of the key, or 
        ///   <see langword="null"/> if the key does not exist.
        /// </returns>
        protected abstract ValueTask<byte[]?> ReadAsync(KVKey key);


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return <see langword="true"/> if the key 
        ///   was deleted, or <see langword="false"/> otherwise.
        /// </returns>
        protected abstract ValueTask<bool> DeleteAsync(KVKey key);


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
        /// Compresses data using GZip compression at the specified compression level.
        /// </summary>
        /// <param name="data">
        ///   The data to compress.
        /// </param>
        /// <param name="compressionLevel">
        ///   The compression level to use.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will compress the data and return the result.
        /// </returns>
        private async Task<byte[]> CompressDataAsync(byte[] data, CompressionLevel compressionLevel) {
            using (var destination = new MemoryStream())
            using (var zip = new GZipStream(destination, compressionLevel, true)) {
                await zip.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                // Need to call Close on GZipStream to ensure that all buffered bytes are written;
                // calling Flush/FlushAsync is not enough!
                zip.Close();
                return destination.ToArray();
            }
        }


        /// <summary>
        /// Decompresses data using GZip compression.
        /// </summary>
        /// <param name="data">
        ///   The data to decompress.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will decompress the data and return the result.
        /// </returns>
        private async Task<byte[]> DecompressDataAsync(byte[] data) {
            using (var source = new MemoryStream(data))
            using (var zip = new GZipStream(source, CompressionMode.Decompress, true))
            using (var destination = new MemoryStream()) {
                await zip.CopyToAsync(destination).ConfigureAwait(false);
                return destination.ToArray();
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



    /// <summary>
    /// Base implementation of <see cref="IKeyValueStore"/>.
    /// </summary>
    /// <remarks>
    ///   Inherit from this class instead of implementing <see cref="IKeyValueStore"/> directly.
    /// </remarks>
    public abstract class KeyValueStore<TOptions> : KeyValueStore where TOptions : KeyValueStoreOptions, new() {

        /// <summary>
        /// The options for the store.
        /// </summary>
        protected TOptions Options { get; }


        /// <summary>
        /// Creates a new <see cref="KeyValueStore"/> with the specified key prefix.
        /// </summary>
        /// <param name="options">
        ///   Store options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        protected KeyValueStore(TOptions? options, ILogger? logger = null) : base(logger) { 
            Options = options ?? new TOptions();
        }


        /// <inheritdoc/>
        protected override CompressionLevel GetCompressionLevel() => Options.CompressionLevel;

    }
}
