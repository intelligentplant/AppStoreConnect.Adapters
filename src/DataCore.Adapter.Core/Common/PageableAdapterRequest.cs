using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Base class for adapter requests that support paging.
    /// </summary>
    [Obsolete("Implement IPageableAdapterRequest directly and supply appropriate validation ranges for PageSize and Page.", true)]
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
