using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.AssetModel.Models {

    /// <summary>
    /// Describes a request to browse nodes in an adapter's asset model.
    /// </summary>
    public class BrowseAssetModelNodesRequest {

        /// <summary>
        /// The ID of the parent node to start at. Specify <see langword="null"/> to request top-level 
        /// nodes.
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The maximum number of hierarchy levels to request. Specify less than one to request all 
        /// levels below the <see cref="ParentId"/>.
        /// </summary>
        public int Depth { get; set; } = 1;

        /// <summary>
        /// The page size for the query.
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// The page number for the query.
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

    }
}
