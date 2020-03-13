using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a request to search for asset model nodes.
    /// </summary>
    public class FindAssetModelNodesRequest : PageableAdapterRequest {

        /// <summary>
        /// The name filter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description filter.
        /// </summary>
        public string Description { get; set; }

    }
}
