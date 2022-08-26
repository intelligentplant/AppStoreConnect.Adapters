using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an adapter feature.
    /// </summary>
    public class FeatureDescriptor {

        /// <summary>
        /// Maximum length of the category name
        /// </summary>
        public const int MaxCategoryLength = 100;

        /// <summary>
        /// Maximum length of the feature display name.
        /// </summary>
        public const int MaxDisplayNameLength = 100;

        /// <summary>
        /// Maximum length of the feature description.
        /// </summary>
        public const int MaxDescriptionLength = 500;

        /// <summary>
        /// The feature URI.
        /// </summary>
        [Required]
        public Uri Uri { get; set; } = default!;

        /// <summary>
        /// The feature category.
        /// </summary>
        [MaxLength(MaxCategoryLength)]
        public string? Category { get; set; }

        /// <summary>
        /// The feature display name.
        /// </summary>
        [Required]
        [MaxLength(MaxDisplayNameLength)]
        public string DisplayName { get; set; } = default!;

        /// <summary>
        /// The feature description.
        /// </summary>
        [MaxLength(MaxDescriptionLength)]
        public string? Description { get; set; }

    }
}
