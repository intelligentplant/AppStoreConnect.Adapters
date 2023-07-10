using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a request to search for asset model nodes.
    /// </summary>
    public class FindAssetModelNodesRequest : AdapterRequest, IPageableAdapterRequest {

        /// <summary>
        /// The name filter.
        /// </summary>
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>
        /// The description filter.
        /// </summary>
        [MaxLength(200)]
        public string? Description { get; set; }

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
