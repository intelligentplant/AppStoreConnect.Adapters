namespace DataCore.Adapter.KeyValueStore.Sqlite {

    /// <summary>
    /// Options for <see cref="SqliteKeyValueStore"/>.
    /// </summary>
    public class SqliteKeyValueStoreOptions : Services.KeyValueStoreOptions {

        /// <summary>
        /// Default Sqlite connection string.
        /// </summary>
        public const string DefaultConnectionString = "Data Source=adapter-kvstore.db";

        /// <summary>
        /// The Sqlite connection string to use.
        /// </summary>
        public string ConnectionString { get; set; } = DefaultConnectionString;

    }

}
