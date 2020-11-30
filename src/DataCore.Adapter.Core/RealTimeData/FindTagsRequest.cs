using System.Collections.Generic;
using System.ComponentModel;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a tag search query.
    /// </summary>
    public sealed class FindTagsRequest : PageableAdapterRequest {

        /// <summary>
        /// The tag name filter.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The tag description filter.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The tag units filter.
        /// </summary>
        public string? Units { get; set; }

        /// <summary>
        /// The tag label filter.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Additional filters on bespoke tag properties.
        /// </summary>
        public IDictionary<string, string>? Other { get; set; }

        /// <summary>
        /// The result fields to populate in the search results.
        /// </summary>
        [DefaultValue(TagDefinitionFields.All)]
        public TagDefinitionFields ResultFields { get; set; } = TagDefinitionFields.All;

    }
}
