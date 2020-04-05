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

    }
}
