
namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an adapter request that specifies a page size and page number.
    /// </summary>
    public interface IPageableAdapterRequest {

        /// <summary>
        /// The page size for the query.
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// The page number for the query.
        /// </summary>
        int Page { get; set; }

    }
}
