using System.Collections.Generic;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// A request to retrieve a filtered list of adapters.
    /// </summary>
    public class FindAdaptersRequest : PageableAdapterRequest {

        /// <summary>
        /// The adapter ID filter. Partial matches can be specified.
        /// </summary>
        /// <remarks>
        ///   Partial matches can be specified; <c>?</c> and <c>*</c> can be used as single- and 
        ///   multi-character wildcards respectively.
        /// </remarks>
        public string? Id { get; set; }

        /// <summary>
        /// The adapter name filter.
        /// </summary>
        /// <remarks>
        ///   Partial matches can be specified; <c>?</c> and <c>*</c> can be used as single- and 
        ///   multi-character wildcards respectively.
        /// </remarks>
        public string? Name { get; set; }

        /// <summary>
        /// The adapter description filter.
        /// </summary>
        /// <remarks>
        ///   Partial matches can be specified; <c>?</c> and <c>*</c> can be used as single- and 
        ///   multi-character wildcards respectively.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// The adapter feature filters.
        /// </summary>
        /// <remarks>
        ///   Unlike the <see cref="Id"/>, <see cref="Name"/>, and <see cref="Description"/> 
        ///   filters, the <see cref="Features"/> filters must exactly match the name of a 
        ///   standard or extension feature.
        /// </remarks>
        public IEnumerable<string>? Features { get; set; }

    }
}
