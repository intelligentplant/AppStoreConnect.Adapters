using System;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Describes a serializer used to serialize and deserialize values in a 
    /// <see cref="FasterKeyValueStore"/>.
    /// </summary>
    public interface IFasterKeyValueStoreSerializer {

        /// <summary>
        /// Serializes the specified item.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The item type.
        /// </typeparam>
        /// <param name="item">
        ///   The item to serialize.
        /// </param>
        /// <returns>
        ///   The serialized item.
        /// </returns>
        byte[] Serialize<TValue>(TValue? item);


        /// <summary>
        /// Deserializes the specified item.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The item value type.
        /// </typeparam>
        /// <param name="bytes">
        ///   The serialized item.
        /// </param>
        /// <returns>
        ///   The deserialized item.
        /// </returns>
        TValue? Deserialize<TValue>(ReadOnlySpan<byte> bytes);

    }
}
