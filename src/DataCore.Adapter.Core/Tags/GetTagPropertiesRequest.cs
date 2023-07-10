using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a request to retrieve tag property definitions.
    /// </summary>
    public class GetTagPropertiesRequest : AdapterRequest, IPageableAdapterRequest {

        /// <inheritdoc/>
        [Range(1, 500)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

    }

}
