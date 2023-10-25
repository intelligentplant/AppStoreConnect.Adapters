using System;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Extends <see cref="IKeyValueStore"/> to allow raw byte data for keys to be read/written.
    /// </summary>
    public interface IRawKeyValueStore : IKeyValueStore {

        /// <summary>
        /// Reads raw byte data for a key.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   The raw byte data for the key, or <see langword="null"/> if the key does not exist.
        /// </returns>
        ValueTask<byte[]?> ReadRawAsync(KVKey key);


        /// <summary>
        /// Writes raw byte data for a key.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="value">
        ///   The raw byte data.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        ValueTask WriteRawAsync(KVKey key, byte[] value);

    }
}
