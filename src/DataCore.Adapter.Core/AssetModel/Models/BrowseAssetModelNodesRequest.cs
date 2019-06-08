using System;
using System.Collections.Generic;
using System.Text;

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

    }
}
