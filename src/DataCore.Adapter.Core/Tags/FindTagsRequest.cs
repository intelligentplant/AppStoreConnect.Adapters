using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a tag search query.
    /// </summary>
    public sealed class FindTagsRequest : AdapterRequest, IPageableAdapterRequest {

        /// <summary>
        /// The tag name filter.
        /// </summary>
        [MaxLength(500)]
        public string? Name { get; set; }

        /// <summary>
        /// The tag description filter.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// The tag units filter.
        /// </summary>
        [MaxLength(50)]
        public string? Units { get; set; }

        /// <summary>
        /// The tag label filter.
        /// </summary>
        [MaxLength(100)]
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

        /// <inheritdoc/>
        [Range(1, 500)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (Other != null) {
                if (Other.Any(x => x.Key.Length > 50)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_KeyIsTooLong, 50), new[] { nameof(Other) });
                }
                if (Other.Any(x => x.Value != null && x.Value.Length > 100)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_ValueIsTooLong, 100), new[] { nameof(Other) });
                }
                if (Other.Count > 20) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TooManyEntries, 20), new[] { nameof(Other) });
                }
            }
        }

    }
}
