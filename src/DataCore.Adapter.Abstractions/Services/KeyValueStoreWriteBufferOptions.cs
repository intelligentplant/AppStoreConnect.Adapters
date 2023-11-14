using System;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Options for <see cref="KeyValueStoreWriteBuffer"/>.
    /// </summary>
    public class KeyValueStoreWriteBufferOptions {

        /// <summary>
        /// The interval at which pending writes are flushed to the store.
        /// </summary>
        /// <remarks>
        ///   If <see cref="FlushInterval"/> is less than or equal to <see cref="TimeSpan.Zero"/> 
        ///   a default flush interval will be used.
        /// </remarks>
        public TimeSpan FlushInterval { get; set; }

        /// <summary>
        /// The maximum number of keys that can be stored in the cache.
        /// </summary>
        /// <remarks>
        ///   If the number of keys in the cache exceeds this value, an immediate flush will be 
        ///   performed. Specify less than one for no limit.
        /// </remarks>
        public int KeyLimit { get; set; }

        /// <summary>
        /// The maximum number of bytes that can be stored in the cache.
        /// </summary>
        /// <remarks>
        ///   If the size of all pending writes exceeds this value, an immediate flush will be 
        ///   performed. Specify less than one for no limit.
        /// </remarks>
        public int SizeLimit { get; set; }

    }
}
