﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.KeyValueStore.Sqlite {

    /// <summary>
    /// <see cref="IKeyValueStore"/> that uses a Sqlite database to store values.
    /// </summary>
    public partial class SqliteKeyValueStore : RawKeyValueStore<SqliteKeyValueStoreOptions>, IDisposable {

        /// <summary>
        /// Sqlite error code when the database file is unavailable.
        /// </summary>
        private const int SQLITE_CANTOPEN = 14;

        /// <summary>
        /// Specifies if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The Sqlite connection string.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Lock for the store.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _lock = new Nito.AsyncEx.AsyncReaderWriterLock();

        /// <summary>
        /// The write buffer, if configured.
        /// </summary>
        private readonly KeyValueStoreWriteBuffer? _writeBuffer;

        /// <summary>
        /// Specifies if the store uses a write buffer.
        /// </summary>
        private bool UseWriteBuffer => _writeBuffer != null;

        /// <summary>
        /// A cancellation token source that is cancelled when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <inheritdoc/>
        protected override bool AllowRawWrites => Options.EnableRawWrites;


        /// <summary>
        /// Creates a new <see cref="SqliteKeyValueStore"/> object.
        /// </summary>
        /// <param name="options">
        ///   The <see cref="SqliteKeyValueStoreOptions"/> for the store.
        /// </param>
        /// <param name="logger">
        ///   The logger factory for the store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public SqliteKeyValueStore(SqliteKeyValueStoreOptions options, ILoggerFactory? logger = null) : base(options, logger?.CreateLogger<SqliteKeyValueStore>()) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _connectionString = string.IsNullOrWhiteSpace(options.ConnectionString)
                ? SqliteKeyValueStoreOptions.DefaultConnectionString
                : options.ConnectionString;

            CreateKVTable();

            if (Options.WriteBuffer?.Enabled ?? false) {
                _writeBuffer = new KeyValueStoreWriteBuffer(options.WriteBuffer, OnFlushAsync, logger?.CreateLogger<KeyValueStoreWriteBuffer>());
            }
            else {
                LogFlushDisabled(Logger);
            }
        }


        /// <summary>
        /// Enables write-ahead logging on a Sqlite connection.
        /// </summary>
        /// <param name="command">
        ///   A command that will execute against the connection to modify.
        /// </param>
        /// <remarks>
        ///   See <a href="https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/async">here</a> for further details.
        /// </remarks>
        private void EnableWriteAheadLogging(SqliteCommand command) {
            command.CommandText = "PRAGMA journal_mode='wal'";
            command.ExecuteNonQuery();
        }


        /// <summary>
        /// Tests if the Sqlite database already exists.
        /// </summary>
        /// <param name="connectionString">
        ///   The connection string.
        /// </param>
        /// <returns>
        ///   A flag indicating if the database already exists.
        /// </returns>
        private bool DatabaseExists(string connectionString) {
            // See here: https://github.com/dotnet/efcore/blob/c918248457a3629736ff50c970ac022917b894b1/src/EFCore.Sqlite.Core/Storage/Internal/SqliteDatabaseCreator.cs#L66

            var connectionOptions = new SqliteConnectionStringBuilder(connectionString);
            if (connectionOptions.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) || connectionOptions.Mode == SqliteOpenMode.Memory) {
                return true;
            }

            var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString) {
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };

            var file = new FileInfo(Path.IsPathRooted(connectionStringBuilder.DataSource) 
                    ? connectionStringBuilder.DataSource 
                    : Path.Combine(AppContext.BaseDirectory, connectionStringBuilder.DataSource));

            file.Directory.Create(); 

            using (var readOnlyConnection = new SqliteConnection(connectionStringBuilder.ToString())) {
                try {
                    readOnlyConnection.Open();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == SQLITE_CANTOPEN) {
                    return false;
                }
            }

            return true;
    }


        /// <summary>
        /// Creates the key-value table in the SQlite database.
        /// </summary>
        private void CreateKVTable() {
            var createDatabase = !DatabaseExists(_connectionString);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (_lock.WriterLock())
                using (var command = connection.CreateCommand()) {
                    if (createDatabase) {
                        EnableWriteAheadLogging(command);
                    }

                    command.CommandText = "CREATE TABLE IF NOT EXISTS kvstore (key TEXT PRIMARY KEY, value BLOB)";
                    command.ExecuteNonQuery();
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask WriteAsync<T>(KVKey key, T value) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            await WriteCoreAsync(key, await SerializeToBytesAsync(value).ConfigureAwait(false)).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask WriteRawAsync(KVKey key, byte[] value) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            await WriteCoreAsync(key, value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a raw value to the store.
        /// </summary>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="data">
        ///   The raw value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        private async ValueTask WriteCoreAsync(KVKey key, byte[] data) {
            using (await _lock.WriterLockAsync().ConfigureAwait(false)) {
                if (UseWriteBuffer && !_disposed) {
                    await _writeBuffer!.WriteAsync(key, data, _disposedTokenSource.Token).ConfigureAwait(false);
                    return;
                }

                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction()) {
                        await WriteCoreAsync(connection, transaction, ConvertBytesToHexString(key), data).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }
            }
        }


        /// <summary>
        /// Writes a raw value to the store.
        /// </summary>
        /// <param name="connection">
        ///   The connection to use.
        /// </param>
        /// <param name="transaction">
        ///   The transaction to use.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="data">
        ///   The raw value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        private async ValueTask WriteCoreAsync(SqliteConnection connection, SqliteTransaction transaction, string key, byte[] data) {
            using (var command = connection.CreateCommand()) {
                command.Transaction = transaction;

                command.CommandText = "INSERT INTO kvstore (key, value) VALUES ($key, zeroblob($length)) ON CONFLICT (key) DO UPDATE SET value = zeroblob($length) RETURNING rowid";
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$length", data.Length);

                var rowId = (long) await command.ExecuteScalarAsync().ConfigureAwait(false);
                using (var writeStream = new SqliteBlob(connection, "kvstore", "value", rowId)) {
#if NETSTANDARD2_1_OR_GREATER
                    await writeStream.WriteAsync(data).ConfigureAwait(false);
#else
                    await writeStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
#endif
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<T?> ReadAsync<T>(KVKey key) where T : default {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var hexKey = ConvertBytesToHexString(key);

            using (await _lock.ReaderLockAsync().ConfigureAwait(false)) {
                if (_disposed) {
                    return default;
                }

                // Check pending writes first.
                if (UseWriteBuffer) {
                    var pendingValue = await _writeBuffer!.ReadAsync(key, _disposedTokenSource.Token).ConfigureAwait(false);
                    if (pendingValue.Found) {
                        return pendingValue.Value == null
                            ? default
                            : await DeserializeFromBytesAsync<T>(pendingValue.Value).ConfigureAwait(false);
                    }
                }

                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "SELECT rowid, value FROM kvstore WHERE key = $key LIMIT 1";
                        command.Parameters.AddWithValue("$key", hexKey);

                        using (var reader = command.ExecuteReader()) {
                            if (!reader.Read()) {
                                return default;
                            }

                            using (var stream = reader.GetStream(1)) {
                                return await DeserializeFromStreamAsync<T>(stream).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<byte[]?> ReadRawAsync(KVKey key) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using (await _lock.ReaderLockAsync().ConfigureAwait(false)) {
                if (_disposed) {
                    return null;
                }

                // Check pending writes first.
                if (UseWriteBuffer) {
                    var pendingValue = await _writeBuffer!.ReadAsync(key, _disposedTokenSource.Token).ConfigureAwait(false);
                    if (pendingValue.Found) {
                        if (pendingValue.Value == null) {
                            return null;
                        }

                        var result = new byte[pendingValue.Value.Length];
                        Array.Copy(pendingValue.Value, result, result.Length);
                        return result;
                    }
                }

                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "SELECT rowid, value FROM kvstore WHERE key = $key LIMIT 1";
                        command.Parameters.AddWithValue("$key", ConvertBytesToHexString(key));

                        using (var reader = command.ExecuteReader()) {
                            if (!reader.Read()) {
                                return null;
                            }

                            using (var stream = reader.GetStream(1))
                            using (var ms = new MemoryStream()) {
                                await stream.CopyToAsync(ms).ConfigureAwait(false);
                                return ms.ToArray();
                            }
                        }
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> ExistsAsync(KVKey key) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using (await _lock.ReaderLockAsync().ConfigureAwait(false)) {
                if (_disposed) {
                    return false;
                }

                // Check pending writes first.
                if (UseWriteBuffer) {
                    var pendingValue = await _writeBuffer!.ReadAsync(key, _disposedTokenSource.Token).ConfigureAwait(false);
                    if (pendingValue.Found) {
                        return pendingValue.Value != null;
                    }
                }

                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "SELECT COUNT(*) FROM kvstore WHERE key = $key";
                        command.Parameters.AddWithValue("$key", ConvertBytesToHexString(key));

                        var count = Convert.ToInt32(await command.ExecuteScalarAsync(_disposedTokenSource.Token).ConfigureAwait(false));
                        return count != 0;
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> DeleteAsync(KVKey key) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            using (await _lock.WriterLockAsync().ConfigureAwait(false)) {
                if (UseWriteBuffer && !_disposed) {
                    await _writeBuffer!.DeleteAsync(key, _disposedTokenSource.Token).ConfigureAwait(false);
                    return true;
                }

                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction()) {
                        var result = await DeleteCoreAsync(connection, transaction, ConvertBytesToHexString(key)).ConfigureAwait(false);
                        transaction.Commit();
                        return result;
                    }
                }
            }
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="connection">
        ///   The connection to use.
        /// </param>
        /// <param name="transaction">
        ///   The transaction to use.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will process the operation.
        /// </returns>
        private async ValueTask<bool> DeleteCoreAsync(SqliteConnection connection, SqliteTransaction transaction, string key) {
            using (var command = connection.CreateCommand()) {
                command.Transaction = transaction;
                command.CommandText = @"DELETE FROM kvstore WHERE key = $key";
                command.Parameters.AddWithValue("$key", key);

                var count = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                return count != 0;
            }
        }


        /// <summary>
        /// Invoked when the write buffer is flushed.
        /// </summary>
        /// <param name="changes">
        ///   The changes to write to the store.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        private async Task OnFlushAsync(IEnumerable<KeyValuePair<KVKey, byte[]?>> changes) {
            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();
                using (var transaction = connection.BeginTransaction()) {
                    foreach (var change in changes) {
                        if (change.Value == null) {
                            await DeleteCoreAsync(connection, transaction, ConvertBytesToHexString(change.Key)).ConfigureAwait(false);
                        }
                        else {
                            await WriteCoreAsync(connection, transaction, ConvertBytesToHexString(change.Key), change.Value).ConfigureAwait(false);
                        }
                    }
                    transaction.Commit();
                }
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            var hexPrefix = prefix == null || prefix.Value.Length == 0 
                ? null 
                : ConvertBytesToHexString(prefix);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (await _lock.ReaderLockAsync().ConfigureAwait(false))
                using (var command = connection.CreateCommand()) {
                    if (hexPrefix == null) {
                        command.CommandText = "SELECT key FROM kvstore";
                    }
                    else {
                        command.CommandText = "SELECT key from kvstore WHERE key LIKE $filter";
                        command.Parameters.AddWithValue("$filter", string.Concat(hexPrefix, "%"));
                    }

                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) { 
                            yield return ConvertHexStringToBytes(reader.GetString(0));
                        }
                    }
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

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _writeBuffer?.Dispose();

            _disposed = true;
        }


        [LoggerMessage(100, LogLevel.Information, "Changes will be flushed to the database immediately.")]
        static partial void LogFlushDisabled(ILogger logger);

    }
}
