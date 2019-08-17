﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
