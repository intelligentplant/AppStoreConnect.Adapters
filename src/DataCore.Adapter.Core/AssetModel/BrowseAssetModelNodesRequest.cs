using DataCore.Adapter.Common;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a request to browse nodes in an adapter's asset model.
    /// </summary>
    public class BrowseAssetModelNodesRequest : PageableAdapterRequest {

        /// <summary>
        /// The ID of the parent node to start at. Specify <see langword="null"/> to request top-level 
        /// nodes.
        /// </summary>
        public string? ParentId { get; set; }


        /// <summary>
        /// Creates a new <see cref="BrowseAssetModelNodesRequest"/> object.
        /// </summary>
        public BrowseAssetModelNodesRequest() {
            PageSize = 100;
        }

    }
}
