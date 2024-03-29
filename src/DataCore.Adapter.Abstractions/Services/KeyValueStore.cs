﻿using System;
using System.Buffers;
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
    ///   Implementations should extend <see cref="KeyValueStore"/> or <see cref="KeyValueStore{TOptions}"/> 
    ///   rather than implementing <see cref="IKeyValueStore"/> directly.
    /// </remarks>
    /// <seealso cref="KeyValueStore{TOptions}"/>
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


        /// <inheritdoc/>
        async ValueTask IKeyValueStore.WriteAsync<T>(KVKey key, T value) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            await WriteAsync(key, value).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async ValueTask<T?> IKeyValueStore.ReadAsync<T>(KVKey key) where T : default {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            return await ReadAsync<T>(key).ConfigureAwait(false); 
        }


        /// <inheritdoc/>
        async ValueTask<bool> IKeyValueStore.ExistsAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            return await ExistsAsync(key).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async ValueTask<bool> IKeyValueStore.DeleteAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            return await DeleteAsync(key).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        IAsyncEnumerable<KVKey> IKeyValueStore.GetKeysAsync(KVKey? prefix) {
            if (prefix != null && prefix.Value.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(prefix));
            }
            return GetKeysAsync(prefix);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will complete when the value has been written.
        /// </returns>
        protected abstract ValueTask WriteAsync<T>(KVKey key, T value);


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="key">
        ///   The key. Guaranteed to have a non-null, non-empty <see cref="KVKey.Value"/>.
        /// </param>
        /// <returns>
        ///   The value, or <see langword="null"/> if the key does not exist.
        /// </returns>
        protected abstract ValueTask<T?> ReadAsync<T>(KVKey key);


        /// <summary>
        /// Tests if a key exists in the store.
        /// </summary>
        /// <param name="key">
        ///   The key to test.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the key exists; otherwise, <see langword="false"/>.
        /// </returns>
        protected abstract ValueTask<bool> ExistsAsync(KVKey key);


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
        /// Gets the default compression level to use for serialized data.
        /// </summary>
        /// <returns>
        ///   The default compression level.
        /// </returns>
        /// <remarks>
        ///   
        /// <para>
        ///   The default compression level is used if no <see cref="CompressionLevel"/> is passed 
        ///   to the following methods:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <see cref="SerializeToBytesAsync{T}(T, CompressionLevel?)"/>
        ///   </item>
        ///   <item>
        ///     <see cref="SerializeToStreamAsync{T}(Stream, T, CompressionLevel?)"/>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        protected abstract CompressionLevel GetCompressionLevel();


        /// <summary>
        /// Gets the serializer for the store.
        /// </summary>
        /// <returns>
        ///   The serializer.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="GetSerializer"/> is used by the following methods:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <see cref="SerializeToBytesAsync{T}(T, CompressionLevel?)"/>
        ///   </item>
        ///   <item>
        ///     <see cref="SerializeToStreamAsync{T}(Stream, T, CompressionLevel?)"/>
        ///   </item>
        ///   <item>
        ///     <see cref="DeserializeFromBytesAsync{T}(byte[])"/>
        ///   </item>
        ///   <item>
        ///     <see cref="DeserializeFromStreamAsync{T}(Stream)"/>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        protected abstract IKeyValueStoreSerializer GetSerializer();


        /// <summary>
        /// Serializes and compresses a value to a byte array.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="value">
        ///   The value to serialize.
        /// </param>
        /// <param name="compressionLevel">
        ///   The compression level to use. If <see langword="null"/>, the value returned by 
        ///   <see cref="GetCompressionLevel"/> is used.
        /// </param>
        /// <returns>
        ///   The serialized value.
        /// </returns>
        protected async ValueTask<byte[]> SerializeToBytesAsync<T>(T value, CompressionLevel? compressionLevel = null) {
            using (var ms = new MemoryStream()) {
                await SerializeToStreamAsync(ms, value, compressionLevel).ConfigureAwait(false);
                return ms.ToArray();
            }
        }


        /// <summary>
        /// Serializes and compresses a value to a stream.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream to write the serialized value to.
        /// </param>
        /// <param name="value">
        ///   The value to serialize.
        /// </param>
        /// <param name="compressionLevel">
        ///   The compression level to use. If <see langword="null"/>, the value returned by 
        ///   <see cref="GetCompressionLevel"/> is used.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will complete when the value has been serialized.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        protected virtual async ValueTask SerializeToStreamAsync<T>(Stream stream, T value, CompressionLevel? compressionLevel = null) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }

            var level = compressionLevel ?? GetCompressionLevel();

            using (var compressStream = new GZipStream(stream, level, leaveOpen: true)) {
                await SerializeToStreamCoreAsync(compressStream, value).ConfigureAwait(false);
                compressStream.Close();
            }
        }


        /// <summary>
        /// Serializes a value to a stream.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream to write the serialized value to.
        /// </param>
        /// <param name="value">
        ///   The value to serialize.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will complete when the value has been serialized.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        protected async ValueTask SerializeToStreamCoreAsync<T>(Stream stream, T value) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            await GetSerializer().SerializeAsync(stream, value).ConfigureAwait(false);
        }


        /// <summary>
        /// Deserializes a value from a compressed byte array.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="data">
        ///   The serialized value.
        /// </param>
        /// <returns>
        ///   The deserialized value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected async ValueTask<T?> DeserializeFromBytesAsync<T>(byte[] data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            using (var ms = new MemoryStream(data)) {
                return await DeserializeFromStreamAsync<T?>(ms).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Deserializes a value from a compressed stream.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream containing the serialized value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the deserialized value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        protected virtual async ValueTask<T?> DeserializeFromStreamAsync<T>(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var decompressStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true)) {
                return await DeserializeFromStreamCoreAsync<T>(decompressStream).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Deserializes a value from a stream.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream containing the serialized value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the deserialized value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        protected async ValueTask<T?> DeserializeFromStreamCoreAsync<T>(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }

            return await GetSerializer().DeserializeAsync<T?>(stream).ConfigureAwait(false);
        }


        /// <summary>
        /// Tests if the specified stream contains gzipped data.
        /// </summary>
        /// <param name="stream">
        ///   The stream.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the first two bytes in the stream are 0x1F and 0x8B; 
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        protected static bool IsGzipped(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }

            try {
                using (var subscription = MemoryPool<byte>.Shared.Rent(2)) {
#if NETSTANDARD2_1_OR_GREATER
                    var bytesRead = stream.Read(subscription.Memory.Span);
                    return bytesRead >= 2 && subscription.Memory.Span[0] == 0x1F && subscription.Memory.Span[1] == 0x8B;
#else
                    var arr = subscription.Memory.Span.ToArray();
                    var bytesRead = stream.Read(arr, 0, 2);
                    return bytesRead == 2 && arr[0] == 0x1F && arr[1] == 0x8B;
#endif
                }
            }
            finally {
                stream.Position = 0;
            }
        }


        /// <summary>
        /// Tests if the specified byte array contains gzipped data.
        /// </summary>
        /// <param name="data">
        ///   The byte array.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the first two bytes in the array are 0x1F and 0x8B; 
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        protected static bool IsGzipped(Span<byte> data) { 
            return data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B;
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

    }

}
