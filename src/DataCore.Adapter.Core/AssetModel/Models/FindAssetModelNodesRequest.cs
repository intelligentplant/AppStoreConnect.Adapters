using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.AssetModel.Models {

    /// <summary>
    /// Describes a request to search for asset model nodes.
    /// </summary>
    public class FindAssetModelNodesRequest {

        /// <summary>
        /// The name filter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description filter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The page size for the search results.
        /// </summary>
        private int _pageSize = 10;

        /// <summary>
        /// The page size for the search results.
        /// </summary>
        public int PageSize {
            get { return _pageSize; }
            set { _pageSize = value < 1 ? 1 : value; }
        }

        /// <summary>
        /// The result page to retrieve.
        /// </summary>
        private int _page = 1;

        /// <summary>
        /// The result page to retrieve.
        /// </summary>
        public int Page {
            get { return _page; }
            set { _page = value < 1 ? 1 : value; }
        }

    }
}
