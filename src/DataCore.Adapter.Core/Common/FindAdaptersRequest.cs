using System.Collections.Generic;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// A request to retrieve a filtered list of adapters.
    /// </summary>
    public class FindAdaptersRequest : PageableAdapterRequest {

        /// <summary>
        /// The adapter ID filter.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The adapter name filter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The adapter description filter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The adapter feature filters.
        /// </summary>
        /// <remarks>
        ///   Unlike the <see cref="Id"/>, <see cref="Name"/>, and <see cref="Description"/> 
        ///   filters, the <see cref="Features"/> filters must exactly match the name of a 
        ///   standard or extension feature.
        /// </remarks>
        public IEnumerable<string> Features { get; set; }

    }
}
