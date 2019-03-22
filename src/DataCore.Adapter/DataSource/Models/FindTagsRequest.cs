using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes a tag search query.
    /// </summary>
    public sealed class FindTagsRequest {

        /// <summary>
        /// The tag name filter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tag description filter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The tag units filter.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Additional filters.
        /// </summary>
        private IDictionary<string, string> _other = new Dictionary<string, string>();

        /// <summary>
        /// Additional filters on bespoke tag properties.
        /// </summary>
        public IDictionary<string, string> Other {
            get { return _other; }
            set { _other = value ?? new Dictionary<string, string>(); }
        }

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
