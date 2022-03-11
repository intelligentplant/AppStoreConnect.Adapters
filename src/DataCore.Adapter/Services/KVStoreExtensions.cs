using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Extensions for <see cref="IKeyValueStore"/>.
    /// </summary>
    public static class KVStoreExtensions {

        /// <summary>
        /// Serializes the specified value to JSON and writes it to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The store.
        /// </param>
        /// <param name="key">
        ///   The key to write to.
        /// </param>
        /// <param name="value">
        ///   The value to write.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will perform the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        public static async ValueTask WriteJsonAsync<TValue>(this IKeyValueStore store, KVKey key, TValue value, JsonSerializerOptions? options = null) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            await store.WriteAsync(key, JsonSerializer.SerializeToUtf8Bytes(value, options)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a JSON byte array from the store and deserializes it to the specified type.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The type to deserialize the JSON bytes to.
        /// </typeparam>
        /// <param name="store">
        ///   The store.
        /// </param>
        /// <param name="key">
        ///   The key to read from.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the operation result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        public static async ValueTask<TValue?> ReadJsonAsync<TValue>(this IKeyValueStore store, KVKey key, JsonSerializerOptions? options = null) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            var result = await store.ReadAsync(key).ConfigureAwait(false);
            return result == null
                ? default
                : JsonSerializer.Deserialize<TValue>(result, options);
        }

    }
}
