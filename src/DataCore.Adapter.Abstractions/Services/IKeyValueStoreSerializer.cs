using System.IO;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// A serializer for key/value store values.
    /// </summary>
    public interface IKeyValueStoreSerializer {

        /// <summary>
        /// Serializes a value to a stream.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream to write to.
        /// </param>
        /// <param name="value">
        ///   The value to serialize.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will serialize the value.
        /// </returns>
        ValueTask SerializeAsync<T>(Stream stream, T value);

        /// <summary>
        /// Deserializes a value from a stream.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream to read from.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the deserialized value.
        /// </returns>
        ValueTask<T?> DeserializeAsync<T>(Stream stream);

    }

}
