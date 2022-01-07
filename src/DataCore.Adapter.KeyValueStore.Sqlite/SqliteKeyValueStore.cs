using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.KeyValueStore.Sqlite {

    /// <summary>
    /// <see cref="IKeyValueStore"/> that uses a Sqlite database to store values.
    /// </summary>
    public class SqliteKeyValueStore : Services.KeyValueStore {

        /// <summary>
        /// The Sqlite connection string.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// The logger for the store.
        /// </summary>
        private readonly ILogger _logger;


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
        public SqliteKeyValueStore(SqliteKeyValueStoreOptions options, ILogger<SqliteKeyValueStore>? logger = null) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? (ILogger) Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            _connectionString = string.IsNullOrWhiteSpace(options.ConnectionString)
                ? SqliteKeyValueStoreOptions.DefaultConnectionString
                : options.ConnectionString;

            CreateKVTable();
        }


        /// <summary>
        /// Creates the key-value table in the SQlite database.
        /// </summary>
        private void CreateKVTable() {
            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

                using (var command = connection.CreateCommand()) {
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS kvstore (key TEXT PRIMARY KEY, value BLOB)";
                    command.ExecuteNonQuery();
                }
            }
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreOperationStatus> WriteAsync(KVKey key, byte[] value) {
            var hexKey = ConvertBytesToHexString(key);

            try {
                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    using (var command = connection.CreateCommand()) {
                        command.Transaction = transaction;

                        command.CommandText = "INSERT INTO kvstore (key, value) VALUES ($key, $value) ON CONFLICT (key) DO UPDATE SET value = $value";
                        command.Parameters.AddWithValue("$key", hexKey);
                        command.Parameters.AddWithValue("$value", value);

                        command.ExecuteNonQuery();
                        transaction.Commit();

                        return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.OK);
                    }
                }
            }
            catch (Exception e) {
                _logger.LogError(e, Resources.Log_ErrorWritingValue, hexKey);
                return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.Error);
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<KeyValueStoreReadResult> ReadAsync(KVKey key) {
            var hexKey = ConvertBytesToHexString(key);

            try {
                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "SELECT value FROM kvstore WHERE key = $key LIMIT 1";
                        command.Parameters.AddWithValue("$key", hexKey);

                        using (var reader = command.ExecuteReader()) {
                            if (!reader.Read()) {
                                return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.NotFound, default);
                            }

                            using (var stream = reader.GetStream(0))
                            using (var ms = new MemoryStream()){
                                await stream.CopyToAsync(ms).ConfigureAwait(false);
                                return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.OK, ms.ToArray());
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                _logger.LogError(e, Resources.Log_ErrorReadingValue, hexKey);
                return new KeyValueStoreReadResult(KeyValueStoreOperationStatus.Error, default);
            }
        }


        /// <inheritdoc/>
        protected override ValueTask<KeyValueStoreOperationStatus> DeleteAsync(KVKey key) {
            var hexKey = ConvertBytesToHexString(key);

            try {
                using (var connection = new SqliteConnection(_connectionString)) {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    using (var command = connection.CreateCommand()) {
                        command.Transaction = transaction;
                        command.CommandText = @"DELETE FROM kvstore WHERE key = $key";
                        command.Parameters.AddWithValue("$key", hexKey);

                        var count = command.ExecuteNonQuery();
                        transaction.Commit();

                        return new ValueTask<KeyValueStoreOperationStatus>(count == 0
                            ? KeyValueStoreOperationStatus.NotFound
                            : KeyValueStoreOperationStatus.OK
                        );
                    }
                }
            }
            catch (Exception e) {
                _logger.LogError(e, Resources.Log_ErrorDeletingValue, hexKey);
                return new ValueTask<KeyValueStoreOperationStatus>(KeyValueStoreOperationStatus.Error);
            }
        }


        /// <inheritdoc/>
        protected override IEnumerable<KVKey> GetKeys(KVKey? prefix) {
            var hexPrefix = prefix == null || prefix.Value.Length == 0 
                ? null 
                : ConvertBytesToHexString(prefix);

            using (var connection = new SqliteConnection(_connectionString)) {
                connection.Open();

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
