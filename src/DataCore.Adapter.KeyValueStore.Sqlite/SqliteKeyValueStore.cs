using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.KeyValueStore.Sqlite {

    /// <summary>
    /// <see cref="IKeyValueStore"/> that uses a Sqlite database to store values.
    /// </summary>
    public class SqliteKeyValueStore : RawKeyValueStore<SqliteKeyValueStoreOptions> {

        /// <summary>
        /// Sqlite error code when the database file is unavailable.
        /// </summary>
        private const int SQLITE_CANTOPEN = 14;

        /// <summary>
        /// The Sqlite connection string.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Lock for the store.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _lock = new Nito.AsyncEx.AsyncReaderWriterLock();

        /// <inheritdoc/>
        protected override bool AllowRawWrites => Options.EnableRawWrites;


        /// <summary>
        /// Creates a new <see cref="SqliteKeyValueStore"/> object.
        /// </summary>
        /// <param name="options">
        ///   The <see cref="SqliteKeyValueStoreOptions"/> for the store.
        /// </param>
        /// <param name="logger">
        ///   The logger for the store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public SqliteKeyValueStore(SqliteKeyValueStoreOptions options, ILogger<SqliteKeyValueStore>? logger = null) : base(options, logger) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _connectionString = string.IsNullOrWhiteSpace(options.ConnectionString)
                ? SqliteKeyValueStoreOptions.DefaultConnectionString
                : options.ConnectionString;

            CreateKVTable();
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
            await WriteCoreAsync(key, await SerializeToBytesAsync(value).ConfigureAwait(false)).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask WriteRawAsync(KVKey key, byte[] value) {
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
            var hexKey = ConvertBytesToHexString(key);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (await _lock.WriterLockAsync().ConfigureAwait(false))
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand()) {
                    command.Transaction = transaction;

                    command.CommandText = "INSERT INTO kvstore (key, value) VALUES ($key, zeroblob($length)) ON CONFLICT (key) DO UPDATE SET value = zeroblob($length) RETURNING rowid";
                    command.Parameters.AddWithValue("$key", hexKey);
                    command.Parameters.AddWithValue("$length", data.Length);

                    var rowId = (long) await command.ExecuteScalarAsync().ConfigureAwait(false);
                    using (var writeStream = new SqliteBlob(connection, "kvstore", "value", rowId)) {
#if NETSTANDARD2_1_OR_GREATER
                        await writeStream.WriteAsync(data).ConfigureAwait(false);
#else
                        await writeStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
#endif
                    }
                    transaction.Commit();
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<T?> ReadAsync<T>(KVKey key) where T : default {
            var hexKey = ConvertBytesToHexString(key);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (await _lock.ReaderLockAsync().ConfigureAwait(false))
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


        /// <inheritdoc/>
        protected override async ValueTask<byte[]?> ReadRawAsync(KVKey key) {
            var hexKey = ConvertBytesToHexString(key);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (await _lock.ReaderLockAsync().ConfigureAwait(false))
                using (var command = connection.CreateCommand()) {
                    command.CommandText = "SELECT rowid, value FROM kvstore WHERE key = $key LIMIT 1";
                    command.Parameters.AddWithValue("$key", hexKey);

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


        /// <inheritdoc/>
        protected override async ValueTask<bool> DeleteAsync(KVKey key) {
            var hexKey = ConvertBytesToHexString(key);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (await _lock.WriterLockAsync().ConfigureAwait(false))
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand()) {
                    command.Transaction = transaction;
                    command.CommandText = @"DELETE FROM kvstore WHERE key = $key";
                    command.Parameters.AddWithValue("$key", hexKey);

                    var count = command.ExecuteNonQuery();
                    transaction.Commit();

                    return count != 0;
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

    }
}
