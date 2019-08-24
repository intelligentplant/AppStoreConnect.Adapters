namespace DataCore.Adapter {

    /// <summary>
    /// Base class for runtime options for adapters deriving from <see cref="AdapterBase{TAdapterOptions}"/>.
    /// </summary>
    public class AdapterOptions {

        /// <summary>
        /// The adapter ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The adapter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        public string Description { get; set; }

    }
}
