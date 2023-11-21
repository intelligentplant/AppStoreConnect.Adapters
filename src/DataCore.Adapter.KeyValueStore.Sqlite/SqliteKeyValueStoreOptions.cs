using DataCore.Adapter.Services;

namespace DataCore.Adapter.KeyValueStore.Sqlite {

    /// <summary>
    /// Options for <see cref="SqliteKeyValueStore"/>.
    /// </summary>
    public class SqliteKeyValueStoreOptions : KeyValueStoreOptions {

        /// <summary>
        /// Default Sqlite connection string.
        /// </summary>
        public const string DefaultConnectionString = "Data Source=adapter-kvstore.db";

        /// <summary>
        /// The Sqlite connection string to use.
        /// </summary>
        public string ConnectionString { get; set; } = DefaultConnectionString;

        /// <summary>
        /// When <see langword="true"/>, enables the use of <see cref="IRawKeyValueStore.WriteRawAsync"/> 
        /// to write raw byte data to the store.
        /// </summary>
        /// <remarks>
        ///   Attempting a raw write will throw an exception if this property is <see langword="false"/>.
        /// </remarks>
        public bool EnableRawWrites { get; set; }

        /// <summary>
        /// The options for the write buffer.
        /// </summary>
        public SqliteKeyValueStoreWriteBufferOptions WriteBuffer { get; set; } = new SqliteKeyValueStoreWriteBufferOptions();

    }


    /// <summary>
    /// Options for <see cref="SqliteKeyValueStore"/> write buffer.
    /// </summary>
    public class SqliteKeyValueStoreWriteBufferOptions : KeyValueStoreWriteBufferOptions {

        /// <summary>
        /// Specifies if the write buffer is enabled.
        /// </summary>
        public bool Enabled { get; set; }

    }

}
