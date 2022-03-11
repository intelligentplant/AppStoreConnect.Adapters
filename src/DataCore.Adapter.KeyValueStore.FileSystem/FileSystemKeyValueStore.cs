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
    public class FileSystemKeyValueStore : KeyValueStore<FileSystemKeyValueStoreOptions>, IDisposable {

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
        /// Cancellation token source that fires when the store is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Cancellation token from <see cref="_disposedTokenSource"/>.
        /// </summary>
        private readonly CancellationToken _disposedToken;

        /// <summary>
        /// The base directory for the persisted files.
        /// </summary>
        private readonly DirectoryInfo _baseDirectory;

        /// <summary>
        /// The number of hash buckets to distribute the files across.
        /// </summary>
        private readonly int _hashBuckets;

        /// <summary>
        /// Holds read/write locks for each individual key (file).
        /// </summary>
        private readonly ConcurrentDictionary<string, AsyncReaderWriterLock> _fileLocks = new ConcurrentDictionary<string, AsyncReaderWriterLock>();

        /// <summary>
        /// Lookup from key to file name.
        /// </summary>
        private ConcurrentDictionary<string, string> _fileIndex = default!;

        /// <summary>
        /// Loads the index from disk on the first call to the store.
        /// </summary>
        private readonly Lazy<Task> _indexLoader;

        /// <summary>
        /// Indicates if an index save is pending.
        /// </summary>
        private int _indexSavePending;

        /// <summary>
        /// Indicates if an index save is in progress.
        /// </summary>
        private readonly ManualResetEventSlim _indexSaveInProgress = new ManualResetEventSlim();


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
        public FileSystemKeyValueStore(FileSystemKeyValueStoreOptions options, ILogger<FileSystemKeyValueStore>? logger = null) : base(options, logger) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _disposedToken = _disposedTokenSource.Token;
            
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
        }


        /// <summary>
        /// Gets the <see cref="AsyncReaderWriterLock"/> for the specified file name.
        /// </summary>
        /// <param name="fileName">
        ///   The file name.
        /// </param>
        /// <returns>
        ///   The associated lock.
        /// </returns>
        private AsyncReaderWriterLock GetOrAddLockForFile(string fileName) {
            return _fileLocks.GetOrAdd(fileName, _ = new AsyncReaderWriterLock());
        }


        /// <summary>
        /// Removes the <see cref="AsyncReaderWriterLock"/> for the specified file name.
        /// </summary>
        /// <param name="fileName">
        ///   The file name.
        /// </param>
        private void RemoveLockForFile(string fileName) {
            _fileLocks.TryRemove(fileName, out _);
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
                var readResult = await ReadFileAsync(IndexFileName).ConfigureAwait(false);

                if (readResult == null || readResult.Length == 0) {
                    _fileIndex = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
                    return;
                }

                var index = JsonSerializer.Deserialize<Dictionary<string, string>>(readResult);
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
        /// <param name="delay">
        ///   The delay to apply before saving the index.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token that will cause the index to be saved immediately, event if 
        ///   the delay has not completed.
        /// </param>
        /// <returns>
        ///   A task that will save the index.
        /// </returns>
        private async Task SaveIndexAsync(TimeSpan delay, CancellationToken cancellationToken) {
            if (_disposed) {
                return;
            }

            _indexSaveInProgress.Reset();

            try {
                // If the cancellation token fires before the delay has completed, we will save
                // the index immediately, as this indicates that the store is being disposed.
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            await SaveIndexCoreAsync().ConfigureAwait(false);
            _indexSaveInProgress.Set();
        }


        /// <summary>
        /// Serializes the index and saves it to disk.
        /// </summary>
        /// <returns>
        ///   A task that will save the index.
        /// </returns>
        private async ValueTask SaveIndexCoreAsync() {
            try {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(_fileIndex);

                // Always use fastest possible compression for index.
                await WriteFileAsync(IndexFileName, bytes, CompressionLevel.Fastest).ConfigureAwait(false);
            }
            catch (Exception e) {
                Logger.LogError(e, Resources.Log_ErrorDuringIndexSave);
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
        /// Gets the file name to use for the specified key.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the name of the file for the key 
        ///   (relative to the base directory).
        /// </returns>
        private async ValueTask<string> GetFileNameForKeyAsync(KVKey key) {
            await _indexLoader.Value.ConfigureAwait(false);
            var keyAsHex = ConvertBytesToHexString(key);

            var dirty = false;
            var result = _fileIndex.GetOrAdd(keyAsHex, _ => {
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

            if (dirty && Interlocked.CompareExchange(ref _indexSavePending, 1, 0) == 0) {
                _ = Task.Run(async () => {
                    try {
                        await SaveIndexAsync(TimeSpan.FromSeconds(1), _disposedToken).ConfigureAwait(false);
                    }
                    finally {
                        _indexSavePending = 0;
                    }
                });
            }

            return result;
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
        private async ValueTask WriteFileAsync(string fileName, byte[] value, CompressionLevel compressionLevel) {
            var @lock = GetOrAddLockForFile(fileName);
            using (await @lock.WriterLockAsync().ConfigureAwait(false)) {
                var file = new FileInfo(Path.Combine(_baseDirectory.FullName, fileName));

                // Ensure that containing directory exists.
                file.Directory.Create();

                if (!file.Exists) {
                    // Fast path when file does not exist yet.
                    await WriteFileAsync(file, value, compressionLevel).ConfigureAwait(false);
                    return;
                }

                // File already exists.

                // Save the data to a temporary file.
                var tempFile = new FileInfo(Path.Combine(_baseDirectory.FullName, string.Concat("~", Guid.NewGuid().ToString(), ".tmp")));
                await WriteFileAsync(tempFile, value, compressionLevel).ConfigureAwait(false);

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
        }


        /// <summary>
        /// Writes a file to disk.
        /// </summary>
        /// <param name="file">
        ///   The file to write.
        /// </param>
        /// <param name="content">
        ///   The content to write.
        /// </param>
        /// <param name="compressionLevel">
        ///   The compression level.
        /// </param>
        /// <returns>
        ///   A task that will perform the write.
        /// </returns>
        private async Task WriteFileAsync(FileInfo file, byte[] content, CompressionLevel compressionLevel) {
            using (var stream = file.OpenWrite())
            using (var gzStream = new GZipStream(stream, compressionLevel, true)) {
                await gzStream.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
                // Calling Flush/FlushAsync on GZipStream does not always seem to flush all
                // pending data; calling Close does.
                gzStream.Close();
            }
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
        private async ValueTask<byte[]?> ReadFileAsync(string fileName) {
            var file = new FileInfo(Path.Combine(_baseDirectory.FullName, fileName));
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));

            if (!file.Exists && !backupFile.Exists) {
                return null;
            }

            var @lock = GetOrAddLockForFile(fileName);
            using (await @lock.ReaderLockAsync().ConfigureAwait(false)) {
                // The file might have been deleted while we were waiting for the lock.
                file.Refresh();
                backupFile.Refresh();

                if (!file.Exists && !backupFile.Exists) {
                    return null;
                }

                // Use the backup file if the original file does not exist.
                var f = file.Exists ? file : backupFile;
                return await ReadFileAsync(f).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Reads a file from disk.
        /// </summary>
        /// <param name="file">
        ///   The file to read.
        /// </param>
        /// <returns>
        ///   The content of the file.
        /// </returns>
        private async Task<byte[]> ReadFileAsync(FileInfo file) {
            using (var stream = file.OpenRead())
            using (var gzStream = new GZipStream(stream, CompressionMode.Decompress, true))
            using (var ms = new MemoryStream()) {
                await gzStream.CopyToAsync(ms).ConfigureAwait(false);
                return ms.ToArray();
            }
        }


        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="FileSystemKeyValueStore"/> will always return <see cref="CompressionLevel.NoCompression"/>, 
        /// as it can apply compression more efficiently when reading/writing files instead.
        /// </remarks>
        protected override CompressionLevel GetCompressionLevel() => CompressionLevel.NoCompression;


        /// <inheritdoc/>
        protected override async ValueTask WriteAsync(KVKey key, byte[] value) {
            var fileName = await GetFileNameForKeyAsync(key).ConfigureAwait(false);
            await WriteFileAsync(fileName, value, Options.CompressionLevel).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask<byte[]?> ReadAsync(KVKey key) {
            await _indexLoader.Value.ConfigureAwait(false);
            var fileName = await GetFileNameForKeyAsync(key).ConfigureAwait(false);
            return await ReadFileAsync(fileName).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> DeleteAsync(KVKey key) {
            await _indexLoader.Value.ConfigureAwait(false);
            var fileName = await GetFileNameForKeyAsync(key).ConfigureAwait(false);
            var file = new FileInfo(Path.Combine(_baseDirectory.FullName, fileName));
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));
            if (!file.Exists && !backupFile.Exists) {
                return false;
            }

            var @lock = GetOrAddLockForFile(fileName);
            using (await @lock.WriterLockAsync().ConfigureAwait(false)) {
                // The file might have been deleted while we were waiting for the lock.
                file.Refresh();
                backupFile.Refresh();

                if (!file.Exists && !backupFile.Exists) {
                    return false;
                }

                try {
                    if (file.Exists) {
                        file.Delete();
                    }

                    if (backupFile.Exists) {
                        backupFile.Delete();
                    }

                    return true;
                }
                finally {
                    RemoveLockForFile(fileName);
                }
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            await _indexLoader.Value.ConfigureAwait(false);

            var prefixAsHex = prefix == null || prefix.Value.Length == 0
                ? null
                : ConvertBytesToHexString(prefix.Value);

            var keys = prefix == null || prefix.Value.Length == 0
                ? _fileIndex.Keys
                : _fileIndex.Keys.Where(x => x.StartsWith(prefixAsHex, StringComparison.Ordinal));

            foreach (var key in keys) {
                yield return ConvertHexStringToBytes(key);
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            // Wait for ongoing save to complete.
            _indexSaveInProgress.Wait();
            _indexSaveInProgress.Dispose();

            _disposed = true;
        }

    }
}
