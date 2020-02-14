using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for runtime options for adapters deriving from <see cref="AdapterBase{TAdapterOptions}"/>.
    /// </summary>
    public class AdapterOptions {

        /// <summary>
        /// The adapter ID. The adapter will generate an ID if this property is 
        /// <see langword="null"/> or white space.
        /// </summary>
        public string Id { get; set; }

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
