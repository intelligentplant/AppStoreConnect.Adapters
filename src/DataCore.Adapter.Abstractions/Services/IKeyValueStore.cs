using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// A service that can be used to store arbitrary key-value pairs.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   <see cref="IKeyValueStore"/> is intended to allow an adapter to store arbitrary 
    ///   key-value data that can be persisted between restarts of the adapter or its host 
    ///   application.
    /// </para>  
    /// 
    /// <para>
    ///   Implementations should extend <see cref="KeyValueStore"/> rather than implementing 
    ///   <see cref="IKeyValueStore"/> directly.
    /// </para>
    /// 
    /// </remarks>
    /// <seealso cref="KeyValueStore"/>
    /// <seealso cref="InMemoryKeyValueStore"/>
    /// <seealso cref="ScopedKeyValueStore"/>
    /// <seealso cref="KeyValueStoreExtensions"/>
    public interface IKeyValueStore {

        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        ValueTask<KeyValueStoreOperationStatus> WriteAsync(KVKey key, byte[] value);


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult"/> 
        ///   containing the operation status and value.
        /// </returns>
        ValueTask<KeyValueStoreReadResult> ReadAsync(KVKey key);


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        ValueTask<KeyValueStoreOperationStatus> DeleteAsync(KVKey key);


        /// <summary>
        /// Gets the keys that are defined in the store.
        /// </summary>
        /// <param name="prefix">
        ///   Only keys beginning with this prefix will be returned.
        /// </param>
        /// <returns>
        ///   The keys.
        /// </returns>
        IEnumerable<KVKey> GetKeys(KVKey? prefix);

    }
}
