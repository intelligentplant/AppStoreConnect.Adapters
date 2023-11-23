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
    ///   Implementations should extend <see cref="KeyValueStore"/> or <see cref="KeyValueStore{TOptions}"/> 
    ///   rather than implementing <see cref="IKeyValueStore"/> directly.
    /// </para>
    /// 
    /// </remarks>
    /// <seealso cref="KeyValueStore"/>
    /// <seealso cref="KeyValueStore{TOptions}"/>
    /// <seealso cref="InMemoryKeyValueStore"/>
    /// <seealso cref="ScopedKeyValueStore"/>
    /// <seealso cref="KeyValueStoreExtensions"/>
    public interface IKeyValueStore {

        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value
        /// </typeparam>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        ValueTask WriteAsync<T>(KVKey key, T value);


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value.
        /// </typeparam>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the value of the key, or 
        ///   <see langword="null"/> if the key does not exist.
        /// </returns>
        ValueTask<T?> ReadAsync<T>(KVKey key);


        /// <summary>
        /// Tests if a key exists in the store.
        /// </summary>
        /// <param name="key">
        ///   The key to test.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the key exists; otherwise, <see langword="false"/>.
        /// </returns>
        ValueTask<bool> ExistsAsync(KVKey key);


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return <see langword="true"/> if the key 
        ///   was deleted, or <see langword="false"/> otherwise.
        /// </returns>
        ValueTask<bool> DeleteAsync(KVKey key);


        /// <summary>
        /// Gets the keys that are defined in the store.
        /// </summary>
        /// <param name="prefix">
        ///   Only keys beginning with this prefix will be returned.
        /// </param>
        /// <returns>
        ///   The keys.
        /// </returns>
        IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix);

    }
}
