namespace DataCore.Adapter.Services {
    /// <summary>
    /// Describes the result of an operation on an <see cref="IKeyValueStore"/>.
    /// </summary>
    public enum KeyValueStoreOperationStatus {

        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        OK = 0,

        /// <summary>
        /// The key specified in the operation was not found.
        /// </summary>
        NotFound = 1,

        /// <summary>
        /// An error occurred during the operation.
        /// </summary>
        Error = 2

    }
}
