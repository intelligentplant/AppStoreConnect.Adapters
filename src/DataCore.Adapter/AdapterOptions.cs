namespace DataCore.Adapter {

    /// <summary>
    /// Base class for runtime options for adapters deriving from <see cref="AdapterBase{TAdapterOptions}"/>.
    /// </summary>
    public class AdapterOptions {

        /// <summary>
        /// The adapter name. If <see langword="null"/> or white space, the adapter ID will be 
        /// used as the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        public string Description { get; set; }

    }
}
