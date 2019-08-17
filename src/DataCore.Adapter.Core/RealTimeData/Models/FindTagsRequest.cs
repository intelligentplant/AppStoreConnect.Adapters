using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

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
        /// Additional filters on bespoke tag properties.
        /// </summary>
        public IDictionary<string, string> Other { get; set; }

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
        [DefaultValue(10)]
        public int Page { get; set; } = 1;

    }
}
