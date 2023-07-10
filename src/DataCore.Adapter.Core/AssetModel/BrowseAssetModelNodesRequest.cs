using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a request to browse nodes in an adapter's asset model.
    /// </summary>
    public class BrowseAssetModelNodesRequest : AdapterRequest, IPageableAdapterRequest {

        /// <summary>
        /// The ID of the parent node to start at. Specify <see langword="null"/> to request top-level 
        /// nodes.
        /// </summary>
        [MaxLength(200)]
        public string? ParentId { get; set; }

        /// <inheritdoc/>
        [Range(1, 500)]
        [DefaultValue(100)]
        public int PageSize { get; set; } = 100;

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

    }
}
