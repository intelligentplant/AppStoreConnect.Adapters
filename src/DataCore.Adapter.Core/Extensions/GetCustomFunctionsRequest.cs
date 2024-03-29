﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to retrieve the available custom functions on an adapter.
    /// </summary>
    public class GetCustomFunctionsRequest : AdapterRequest, IPageableAdapterRequest {

        /// <summary>
        /// The ID filter to apply to the functions.
        /// </summary>
        [MaxLength(100)]
        public string? Id { get; set; }

        /// <summary>
        /// The name filter to apply to the functions.
        /// </summary>
        [MaxLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// The description filter to apply to the functions.
        /// </summary>
        [MaxLength(100)]
        public string? Description { get; set; }

        /// <inheritdoc/>
        [Range(1, 100)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

    }

}
