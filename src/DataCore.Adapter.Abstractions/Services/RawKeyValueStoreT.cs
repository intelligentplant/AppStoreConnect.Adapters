using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Base implementation of <see cref="IRawKeyValueStore"/>.
    /// </summary>
    /// <typeparam name="TOptions">
    ///   The store options type.
    /// </typeparam>
    public abstract class RawKeyValueStore<TOptions> : KeyValueStore<TOptions>, IRawKeyValueStore where TOptions : KeyValueStoreOptions, new() {

        /// <summary>
        /// Specifies if raw writes are allowed.
        /// </summary>
        protected abstract bool AllowRawWrites { get; }


        /// <summary>
        /// Creates a new <see cref="RawKeyValueStore{TOptions}"/>.
        /// </summary>
        /// <param name="options">
        ///   Store options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        protected RawKeyValueStore(TOptions? options, ILogger? logger = null) : base(options, logger) { }


        /// <inheritdoc/>
        async ValueTask<byte[]?> IRawKeyValueStore.ReadRawAsync(KVKey key) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }

            var data = await ReadRawAsync(key).ConfigureAwait(false);
            if (data == null) {
                return null;
            }

            return await DecompressRawBytesAsync(data).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async ValueTask IRawKeyValueStore.WriteRawAsync(KVKey key, byte[] value) {
            if (key.Length == 0) {
                throw new ArgumentException(AbstractionsResources.Error_KeyValueStore_InvalidKey, nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (!AllowRawWrites) {
                throw new InvalidOperationException(AbstractionsResources.Error_KeyValueStore_RawWritesDisabled);
            }

            var data = await CompressRawBytesAsync(value, GetCompressionLevel()).ConfigureAwait(false);
            await WriteRawAsync(key, data).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads raw byte data for a key.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   The raw byte data for the key, or <see langword="null"/> if the key does not exist.
        /// </returns>
        protected abstract ValueTask<byte[]?> ReadRawAsync(KVKey key);


        /// <summary>
        /// Writes raw byte data to a key.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="value">
        ///   The raw byte data.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        protected abstract ValueTask WriteRawAsync(KVKey key, byte[] value);


        /// <summary>
        /// Compresses raw byte data.
        /// </summary>
        /// <param name="data">
        ///   The raw byte data.
        /// </param>
        /// <param name="compressionLevel">
        ///   The compression level to use. If <see langword="null"/>, the default compression 
        ///   level is used.
        /// </param>
        /// <returns>
        ///   A byte array containing the compressed raw data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected virtual async ValueTask<byte[]> CompressRawBytesAsync(byte[] data, CompressionLevel? compressionLevel = null) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }

            if (IsGzipped(data)) {
                return data;
            }

            var level = compressionLevel ?? GetCompressionLevel();

            using (var ms = new MemoryStream())
            using (var compressStream = new GZipStream(ms, level, leaveOpen: true)) {
                await compressStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                compressStream.Close();
                return ms.ToArray();
            }
        }


        /// <summary>
        /// Decompresses raw byte data.
        /// </summary>
        /// <param name="data">
        ///   The compressed raw byte data.
        /// </param>
        /// <returns>
        ///   The decompressed raw byte data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected virtual async ValueTask<byte[]> DecompressRawBytesAsync(byte[] data) {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }

            if (!IsGzipped(data)) {
                return data;
            }

            using (var ms1 = new MemoryStream(data))
            using (var decompressStream = new GZipStream(ms1, CompressionMode.Decompress, leaveOpen: true))
            using (var ms2 = new MemoryStream()) {
                await decompressStream.CopyToAsync(ms2).ConfigureAwait(false);
                return ms2.ToArray();
            }
        }

    }
}
