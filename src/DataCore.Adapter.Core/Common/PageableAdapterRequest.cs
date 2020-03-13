using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Base class for adapter requests that support paging.
    /// </summary>
    public abstract class PageableAdapterRequest : AdapterRequest, IPageableAdapterRequest {

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

    }
}
