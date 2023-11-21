using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.Extensions.Logging;

using Nito.AsyncEx;

namespace DataCore.Adapter.KeyValueStore.FileSystem {

    /// <summary>
    /// <see cref="IKeyValueStore"/> implementation that persists files to disk.
    /// </summary>
    public sealed partial class FileSystemKeyValueStore : KeyValueStore<FileSystemKeyValueStoreOptions>, IDisposable {

        /// <summary>
        /// SHA256 for creating distinct, fixed-length file names.
        /// </summary>
        private static readonly SHA256 s_hash = SHA256.Create();

        /// <summary>
        /// File that persists the lookup index.
        /// </summary>
        private const string IndexFileName = "index.data";

        /// <summary>
        /// Flags if the store has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The base directory for the persisted files.
        /// </summary>
        private readonly DirectoryInfo _baseDirectory;

        /// <summary>
        /// The number of hash buckets to distribute the files across.
        /// </summary>
        private readonly int _hashBuckets;

        /// <summary>
        /// Lock for the store.
        /// </summary>
        private readonly AsyncReaderWriterLock _lock = new AsyncReaderWriterLock();

        /// <summary>
        /// Lookup from key to file name.
        /// </summary>
        private ConcurrentDictionary<string, string> _fileIndex = default!;

        /// <summary>
        /// Loads the index from disk on the first call to the store.
        /// </summary>
        private readonly Lazy<Task> _indexLoader;

        /// <summary>
        /// Write buffer for the store, if configured.
        /// </summary>
        private readonly KeyValueStoreWriteBuffer? _writeBuffer;

        /// <summary>
        /// Specifies if the store uses a write buffer.
        /// </summary>
        private bool UseWriteBuffer => _writeBuffer != null;


        /// <summary>
        /// Creates a new <see cref="FileSystemKeyValueStore"/> object.
        /// </summary>
        /// <param name="options">
        ///   The <see cref="FileSystemKeyValueStoreOptions"/> for the store.
        /// </param>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public FileSystemKeyValueStore(FileSystemKeyValueStoreOptions options, ILoggerFactory? logger = null) : base(options, logger?.CreateLogger<FileSystemKeyValueStore>()) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            var path = string.IsNullOrWhiteSpace(options.Path)
                ? FileSystemKeyValueStoreOptions.DefaultPath
                : options.Path;

            if (!Path.IsPathRooted(path)) {
                path = Path.Combine(AppContext.BaseDirectory, path);
            }
            _baseDirectory = new DirectoryInfo(path);
            _hashBuckets = options.HashBuckets <= 0 
                ? FileSystemKeyValueStoreOptions.DefaultHashBuckets 
                : options.HashBuckets;
            
            CleanUpTempFiles();
            RestoreBackupFiles();

            _indexLoader = new Lazy<Task>(async () => {
                await LoadIndexAsync().ConfigureAwait(false);
            }, LazyThreadSafetyMode.ExecutionAndPublication);

            if (Options.WriteBuffer?.Enabled ?? false) {
                _writeBuffer = new KeyValueStoreWriteBuffer(options.WriteBuffer, OnFlushAsync, logger?.CreateLogger<KeyValueStoreWriteBuffer>());
            }
            else {
                LogFlushDisabled(Logger);
            }
        }


        /// <summary>
        /// Converts the specified bytes to base64url encoding.
        /// </summary>
        /// <param name="bytes">
        ///   The bytes.
        /// </param>
        /// <returns>
        ///   The base64url representation of the bytes.
        /// </returns>
        private string ToBase64Url(byte[] bytes) {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('/', '_').Replace('+', '-');
        }


        /// <summary>
        /// Deletes all temporary files that have not yet been deleted.
        /// </summary>
        private void CleanUpTempFiles() {
            if (!_baseDirectory.Exists) {
                return;
            }

            foreach (var file in _baseDirectory.GetFiles("*.tmp", SearchOption.AllDirectories)) {
                file.Delete();
            }
        }


        /// <summary>
        /// Restores all backup files.
        /// </summary>
        private void RestoreBackupFiles() {
            if (!_baseDirectory.Exists) {
                return;
            }

            foreach (var file in _baseDirectory.GetFiles("*.bak", SearchOption.AllDirectories)) {
                // Remove the .bak extension to restore the original file.
                file.MoveTo(file.FullName.Substring(0, file.FullName.Length - 4));
            }
        }


        /// <summary>
        /// Loads the file index.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will load the file index.
        /// </returns>
        private async ValueTask LoadIndexAsync() {
            try {
                var index = await ReadFromFileAsync<IDictionary<string, string>>(IndexFileName).ConfigureAwait(false);

                if (index == null || index.Count == 0) {
                    _fileIndex = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
                    return;
                }

                _fileIndex = new ConcurrentDictionary<string, string>(index, StringComparer.Ordinal);
            }
            catch (Exception e) {
                Logger.LogError(e, Resources.Log_ErrorDuringIndexLoad);
                _fileIndex = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            }
        }


        /// <summary>
        /// Serializes the index and saves it to disk.
        /// </summary>
        /// <returns>
        ///   A task that will save the index.
        /// </returns>
        private async ValueTask SaveIndexAsync() {
            try {
                // Always use fastest possible compression for index.
                await WriteToFileAsync(IndexFileName, _fileIndex, CompressionLevel.Fastest).ConfigureAwait(false);
            }
            catch (Exception e) {
                Logger.LogError(e, Resources.Log_ErrorDuringIndexSave);
            }
        }


        private async Task OnFlushAsync(IEnumerable<KeyValuePair<KVKey, byte[]?>> changes) {
            await _indexLoader.Value.ConfigureAwait(false);

            var indexModified = false;

            using (await _lock.WriterLockAsync().ConfigureAwait(false)) {
                try {
                    foreach (var change in changes) {
                        if (change.Value == null) {
                            if (TryRemoveKeyFromIndex(change.Key, out var fileName)) {
                                indexModified = true;
                                DeleteFile(fileName!);
                            }
                        }
                        else {
                            if (TryAddKeyToIndex(change.Key, out var fileName)) {
                                indexModified = true;
                            }
                            await WriteBytesToFileAsync(fileName, change.Value).ConfigureAwait(false);
                        }
                    }
                }
                finally {
                    if (indexModified) {
                        await SaveIndexAsync().ConfigureAwait(false);
                    }
                }
            }
        }


        private bool TryGetFileNameForKey(KVKey key, out string? fileName) {
            var keyAsHex = ConvertBytesToHexString(key);
            return _fileIndex.TryGetValue(keyAsHex, out fileName);
        }


        private bool TryAddKeyToIndex(KVKey key, out string fileName) {
            var keyAsHex = ConvertBytesToHexString(key);

            var dirty = false;
            fileName = _fileIndex.GetOrAdd(keyAsHex, _ => {
                dirty = true;
                lock (s_hash) {
                    return Path.Combine(
                        Math.Abs((keyAsHex.GetHashCode() % _hashBuckets)).ToString("X"),
                        string.Concat(
                            ToBase64Url(s_hash.ComputeHash(key)),
                            ".data"
                        )
                    );
                }
            });

            return dirty;
        }


        private bool TryRemoveKeyFromIndex(KVKey key, out string? fileName) {
            var keyAsHex = ConvertBytesToHexString(key);
            return _fileIndex.TryRemove(keyAsHex, out fileName);
        }


        private string GetFullPathForDataFileName(string fileName) {
            return Path.Combine(_baseDirectory.FullName, fileName);
        }


        private async ValueTask WriteToFileCoreAsync(string fileName, Func<FileInfo, ValueTask> callback) {
            var file = new FileInfo(GetFullPathForDataFileName(fileName));

            // Ensure that containing directory exists.
            file.Directory.Create();

            if (!file.Exists) {
                // Fast path when file does not exist yet.
                await callback(file).ConfigureAwait(false);
                return;
            }

            // File already exists.

            // Save the data to a temporary file.
            var tempFile = new FileInfo(GetFullPathForDataFileName(string.Concat("~", Guid.NewGuid().ToString(), ".tmp")));
            await callback(tempFile).ConfigureAwait(false);

            // We will create a backup of the original file prior to writing to it in case
            // something goes wrong.
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // On Windows, we can use FileInfo.Replace to replace the original file with
                // the temporary file, creating a backup of the original file in the process.
                tempFile.Replace(file.FullName, backupFile.FullName, true);
            }
            else {
                // FileInfo.Replace throws PlatformNotSupportedException on non-Windows platforms,
                // so we have to perform a similar process ourselves.

                // Create a backup copy of the original file.
                file.CopyTo(backupFile.FullName, true);

                // Replace the original file with the temporary file.
                tempFile.CopyTo(file.FullName, true);

                // Delete the temporary file.
                tempFile.Refresh();
                tempFile.Delete();
            }

            // We can now safely delete the backup copy.
            backupFile.Refresh();
            backupFile.Delete();
        }


        /// <summary>
        /// Writes a file to disk.
        /// </summary>
        /// <param name="fileName">
        ///   The file name (relative to <see cref="_baseDirectory"/>).
        /// </param>
        /// <param name="value">
        ///   The file content.
        /// </param>
        /// <param name="compressionLevel">
        ///   The GZip compression level to use.
        /// </param>
        /// <returns>
        ///   The operation status.
        /// </returns>
        private async ValueTask WriteToFileAsync<T>(string fileName, T value, CompressionLevel? compressionLevel = null) {
            await WriteToFileCoreAsync(fileName, async file => {
                using (var stream = file.OpenWrite()) {
                    await SerializeToStreamAsync(stream, value, compressionLevel: compressionLevel).ConfigureAwait(false);
                }
            });
        }


        private async ValueTask WriteBytesToFileAsync(string fileName, byte[] bytes) {
            await WriteToFileCoreAsync(fileName, async file => {
                using (var stream = file.OpenWrite()) {
                    await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            });
        }


        private bool DeleteFile(string fileName) {
            var file = new FileInfo(GetFullPathForDataFileName(fileName));
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));
            if (!file.Exists && !backupFile.Exists) {
                return false;
            }

            // The file might have been deleted while we were waiting for the lock.
            file.Refresh();
            backupFile.Refresh();

            if (!file.Exists && !backupFile.Exists) {
                return false;
            }

            if (file.Exists) {
                file.Delete();
            }

            if (backupFile.Exists) {
                backupFile.Delete();
            }

            return true;
        }


        /// <summary>
        /// Reads a file from disk.
        /// </summary>
        /// <param name="fileName">
        ///   The file name (relative to <see cref="_baseDirectory"/>).
        /// </param>
        /// <returns>
        ///   The content of the file.
        /// </returns>
        private async ValueTask<T?> ReadFromFileAsync<T>(string fileName) {
            var file = new FileInfo(GetFullPathForDataFileName(fileName));
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));

            if (!file.Exists && !backupFile.Exists) {
                return default;
            }

            // The file might have been deleted while we were waiting for the lock.
            file.Refresh();
            backupFile.Refresh();

            if (!file.Exists && !backupFile.Exists) {
                return default;
            }

            // Use the backup file if the original file does not exist.
            var f = file.Exists ? file : backupFile;
            using (var stream = f.OpenRead()) {
                return await DeserializeFromStreamAsync<T>(stream).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask WriteAsync<T>(KVKey key, T value) {
            if (UseWriteBuffer) {
                await _writeBuffer!.WriteAsync(key, await SerializeToBytesAsync(value).ConfigureAwait(false)).ConfigureAwait(false);
                return;
            }

            await _indexLoader.Value.ConfigureAwait(false);
            using (await _lock.WriterLockAsync().ConfigureAwait(false)) {
                if (TryAddKeyToIndex(key, out var fileName)) {
                    await SaveIndexAsync();
                }
                await WriteToFileAsync(fileName, value, null).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<T?> ReadAsync<T>(KVKey key) where T : default {
            if (UseWriteBuffer) {
                var readResult = await _writeBuffer!.ReadAsync(key).ConfigureAwait(false);
                if (readResult.Found) {
                    return readResult.Value == null
                        ? default
                        : await DeserializeFromBytesAsync<T>(readResult.Value).ConfigureAwait(false);
                }
            }

            await _indexLoader.Value.ConfigureAwait(false);
            using (await _lock.ReaderLockAsync().ConfigureAwait(false)) {
                if (!TryGetFileNameForKey(key, out var fileName)) {
                    return default;
                }
                return await ReadFromFileAsync<T>(fileName!).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> DeleteAsync(KVKey key) {
            if (UseWriteBuffer) {
                await _writeBuffer!.DeleteAsync(key).ConfigureAwait(false);
                return true;
            }

            await _indexLoader.Value.ConfigureAwait(false);
            using (await _lock.WriterLockAsync().ConfigureAwait(false)) {
                if (TryRemoveKeyFromIndex(key, out var fileName)) {
                    await SaveIndexAsync();
                    return DeleteFile(fileName!);
                }

                return false;
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            await _indexLoader.Value.ConfigureAwait(false);

            var prefixAsHex = prefix == null || prefix.Value.Length == 0
                ? null
                : ConvertBytesToHexString(prefix.Value);

            using (await _lock.ReaderLockAsync().ConfigureAwait(false)) {
                var keys = prefix == null || prefix.Value.Length == 0
                    ? _fileIndex.Keys
                    : _fileIndex.Keys.Where(x => x.StartsWith(prefixAsHex, StringComparison.Ordinal));

                foreach (var key in keys) {
                    yield return ConvertHexStringToBytes(key);
                }
            }
        }


        /// <summary>
        /// Flushes pending writes to the database.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        /// <remarks>
        ///   Calling <see cref="FlushAsync"/> has no effect if the store is not configured to use 
        ///   a write buffer.
        /// </remarks>
        public async ValueTask FlushAsync() {
            if (!UseWriteBuffer || _disposed) {
                return;
            }

            await _writeBuffer!.FlushAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// Waits until pending writes have been flushed to the database.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        ///   <see cref="WaitForNextFlushAsync"/> will return immediately if the store is not 
        ///   configured to use a write buffer.
        /// </remarks>
        public async ValueTask WaitForNextFlushAsync(CancellationToken cancellationToken = default) {
            if (!UseWriteBuffer || _disposed) {
                return;
            }

            await _writeBuffer!.WaitForNextFlushAsync(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _writeBuffer?.Dispose();

            _disposed = true;
        }


        [LoggerMessage(100, LogLevel.Information, "Changes will be flushed to the file system immediately.")]
        static partial void LogFlushDisabled(ILogger logger);

    }
}
