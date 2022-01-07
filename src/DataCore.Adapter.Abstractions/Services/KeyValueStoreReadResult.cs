namespace DataCore.Adapter.Services {

    /// <summary>
    /// Describes the result of a read operation on an <see cref="IKeyValueStore"/>.
    /// </summary>
    public struct KeyValueStoreReadResult {

        /// <summary>
        /// The operation status.
        /// </summary>
        public KeyValueStoreOperationStatus Status { get; }

        /// <summary>
        /// The value that was read from the <see cref="IKeyValueStore"/>.
        /// </summary>
        public byte[]? Value { get; }


        /// <summary>
        /// Creates a new <see cref="KeyValueStoreReadResult{T}"/>.
        /// </summary>
        /// <param name="status">
        ///   The operation status.
        /// </param>
        /// <param name="value">
        ///   The value that was read from the <see cref="IKeyValueStore"/>.
        /// </param>
        public KeyValueStoreReadResult(KeyValueStoreOperationStatus status, byte[]? value) {
            Status = status;
            Value = value;
        }

    }


    /// <summary>
    /// Describes the result of a read operation on an <see cref="IKeyValueStore"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The value type of the result.
    /// </typeparam>
    public struct KeyValueStoreReadResult<T> {

        /// <summary>
        /// The operation status.
        /// </summary>
        public KeyValueStoreOperationStatus Status { get; }

        /// <summary>
        /// The value that was read from the <see cref="IKeyValueStore"/>.
        /// </summary>
        public T? Value { get; }


        /// <summary>
        /// Creates a new <see cref="KeyValueStoreReadResult{T}"/>.
        /// </summary>
        /// <param name="status">
        ///   The operation status.
        /// </param>
        /// <param name="value">
        ///   The value that was read from the <see cref="IKeyValueStore"/>.
        /// </param>
        public KeyValueStoreReadResult(KeyValueStoreOperationStatus status, T? value) {
            Status = status;
            Value = value;
        }

    }

}
