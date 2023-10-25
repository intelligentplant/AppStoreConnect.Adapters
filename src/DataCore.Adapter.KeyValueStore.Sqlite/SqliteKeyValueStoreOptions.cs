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

        /// <summary>
        /// When <see langword="true"/>, enables the use of <see cref="Services.IRawKeyValueStore.WriteRawAsync"/> 
        /// to write raw byte data to the store.
        /// </summary>
        /// <remarks>
        ///   Attempting a raw write will throw an exception if this property is <see langword="false"/>.
        /// </remarks>
        public bool EnableRawWrites { get; set; }

    }

}
