using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.Extensions.Logging;

using Nito.AsyncEx;

namespace DataCore.Adapter.KeyValueStore.FileSystem {

    /// <summary>
    /// <see cref="IKeyValueStore"/> implementation that persists files to disk.
    /// </summary>
    public class KeyValueFileStore : Services.KeyValueStore {


        /// <summary>
        /// The base directory for the persisted files.
        /// </summary>
        private readonly DirectoryInfo _baseDirectory;

        /// <summary>
        /// The logger for the store.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Holds read/write locks for each individual key (file).
        /// </summary>
        private readonly ConcurrentDictionary<string, AsyncReaderWriterLock> _fileLocks = new ConcurrentDictionary<string, AsyncReaderWriterLock>();


        /// <summary>
        /// Creates a new <see cref="KeyValueFileStore"/> object.
        /// </summary>
        /// <param name="options">
        ///   The <see cref="KeyValueFileStoreOptions"/> for the store.
        /// </param>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public KeyValueFileStore(KeyValueFileStoreOptions options, ILogger<KeyValueFileStore>? logger = null) : base() {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? (ILogger) Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            var path = string.IsNullOrWhiteSpace(options.Path)
                ? KeyValueFileStoreOptions.DefaultPath
                : options.Path;

            if (!Path.IsPathRooted(path)) {
                path = Path.Combine(AppContext.BaseDirectory, path);
            }
            _baseDirectory = new DirectoryInfo(path);
            _baseDirectory.Create();

            CleanUpTempFiles();
            RestoreBackupFiles();
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
        /// Gets the file name to use for the specified key without the file extension.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   The file name without the extension.
        /// </returns>
        private string GetFileNameForKeyNoExtension(KVKey key) {
            return ConvertBytesToHexString(key);
        }


        /// <summary>
        /// Gets the file name to use for the specified key.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   The file name.
        /// </returns>
        private string GetFileNameForKey(KVKey key) {
            return string.Concat(GetFileNameForKeyNoExtension(key), ".data");
        }


        /// <summary>
        /// Gets the key associated with the specified file.
        /// </summary>
        /// <param name="file">
        ///   The file.
        /// </param>
        /// <returns>
        ///   The key for the file.
        /// </returns>
        private KVKey GetKeyFromFileName(FileInfo file) {
            var fileNameNoExtension = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
            return ConvertHexStringToBytes(fileNameNoExtension);
        }


        private void CleanUpTempFiles() {
            foreach (var file in _baseDirectory.GetFiles("*.tmp")) {
                file.Delete();
            }
        }


        private void RestoreBackupFiles() {
            foreach (var file in _baseDirectory.GetFiles("*.bak")) {
                // Remove the .bak extension to restore the original file.
                file.MoveTo(file.FullName.Substring(0, file.FullName.Length - 4));
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<KeyValueStoreOperationStatus> WriteAsync(KVKey key, byte[] value) {
            var fileName = GetFileNameForKey(key);

            var @lock = GetOrAddLockForFile(fileName);
            using (await @lock.WriterLockAsync().ConfigureAwait(false)) {
                var file = new FileInfo(Path.Combine(_baseDirectory.FullName, fileName));

                try {
                    // Ensure that containing directory exists.
                    file.Directory.Create();

                    if (!file.Exists) {
                        // Fast path when file does not exist yet.
                        using (var stream = file.OpenWrite()) {
                            await stream.WriteAsync(value, 0, value.Length).ConfigureAwait(false);
                            await stream.FlushAsync().ConfigureAwait(false);
                        }

                        return KeyValueStoreOperationStatus.OK;
                    }

                    // File already exists.

                    // Save the data to a temporary file.
                    var tempFile = new FileInfo(Path.Combine(_baseDirectory.FullName, string.Concat("~", Guid.NewGuid().ToString(), ".tmp")));
                    using (var stream = tempFile.OpenWrite()) {
                        await stream.WriteAsync(value, 0, value.Length).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);
                    }

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

                    return KeyValueStoreOperationStatus.OK;

                }
                catch (Exception e) {
                    _logger.LogError(e, Resources.Log_ErrorWritingToFile, file.FullName);
                    return KeyValueStoreOperationStatus.Error;
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<KeyValueStoreReadResult> ReadAsync(KVKey key) {
            var fileName = GetFileNameForKey(key);
            var file = new FileInfo(Path.Combine(_baseDirectory.FullName, fileName));
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));

            if (!file.Exists && !backupFile.Exists) {
                return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.NotFound, default);
            }

            var @lock = GetOrAddLockForFile(fileName);
            using (await @lock.ReaderLockAsync().ConfigureAwait(false)) {
                // The file might have been deleted while we were waiting for the lock.
                file.Refresh();
                backupFile.Refresh();

                if (!file.Exists && !backupFile.Exists) {
                    return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.NotFound, default);
                }

                // Use the backup file if the original file does not exist.
                var f = file.Exists ? file : backupFile;

                try {
                    using (var stream = f.OpenRead())
                    using (var ms = new MemoryStream()) {
                        await stream.CopyToAsync(ms).ConfigureAwait(false);
                        return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.OK, ms.ToArray());
                    }
                }
                catch (Exception e) {
                    _logger.LogError(e, Resources.Log_ErrorReadingFromFile, f.FullName);
                    return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.Error, default);
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(KVKey key) {
            var fileName = GetFileNameForKey(key);
            var file = new FileInfo(Path.Combine(_baseDirectory.FullName, fileName));
            var backupFile = new FileInfo(string.Concat(file.FullName, ".bak"));
            if (!file.Exists && !backupFile.Exists) {
                return KeyValueStoreOperationStatus.NotFound;
            }

            var @lock = GetOrAddLockForFile(fileName);
            using (await @lock.WriterLockAsync().ConfigureAwait(false)) {
                // The file might have been deleted while we were waiting for the lock.
                file.Refresh();
                backupFile.Refresh();

                if (!file.Exists && !backupFile.Exists) {
                    return KeyValueStoreOperationStatus.NotFound;
                }

                try {
                    var result = KeyValueStoreOperationStatus.OK;

                    if (file.Exists) {
                        try {
                            file.Delete();
                        }
                        catch (Exception e) {
                            _logger.LogError(e, Resources.Log_ErrorDeletingFile, file.FullName);
                            result = KeyValueStoreOperationStatus.Error;
                        }
                    }

                    if (backupFile.Exists) {
                        try {
                            backupFile.Delete();
                        }
                        catch (Exception e) {
                            _logger.LogError(e, Resources.Log_ErrorDeletingFile, backupFile.FullName);
                            result = KeyValueStoreOperationStatus.Error;
                        }
                    }

                    return result;
                }
                finally {
                    RemoveLockForFile(fileName);
                }
            }
        }


        /// <inheritdoc/>
        protected override IEnumerable<KVKey> GetKeys(KVKey? prefix) {
            var files = prefix == null || prefix.Value.Length == 0
                ? _baseDirectory.EnumerateFiles("*.data")
                : _baseDirectory.EnumerateFiles($"{GetFileNameForKeyNoExtension(prefix.Value)}*.data");

            foreach (var file in files) {
                if (file.Name.Length == file.Extension.Length) {
                    // This file is just called .json
                    continue;
                }

                // Get key from the file name.
                var key = GetKeyFromFileName(file);
                if (key.Length == 0) {
                    continue;
                }

                yield return key;
            }
        }

    }
}
