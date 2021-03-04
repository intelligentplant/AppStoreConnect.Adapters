using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Extension methods for <see cref="TagDefinition"/>.
    /// </summary>
    public static class TagDefinitionExtensions {

        /// <summary>
        /// Tests if the tag matches a search filter.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="filter">
        ///   The tag search filter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag matches the filter, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool MatchesFilter(this TagDefinition tag, FindTagsRequest filter) {
            if (tag == null || filter == null) {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filter.Name)) {
                if (!tag.Name.Like(filter.Name)) {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Description)) {
                if (!tag.Description.Like(filter.Description)) {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Units)) {
                if (!tag.Units.Like(filter.Units)) {
                    return false;
                }
            }

            if (filter.Other != null) {
                foreach (var item in filter.Other) {
                    if (string.IsNullOrWhiteSpace(item.Value)) {
                        continue;
                    }

                    var prop = tag.Properties?.FirstOrDefault(p => string.Equals(p.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                    if (prop == null) {
                        return false;
                    }

                    if (!Convert.ToString(prop.Value, CultureInfo.InvariantCulture).Like(item.Value)) {
                        return false;
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Applies filter terms to the specified tags and selects a page of results.
        /// </summary>
        /// <param name="tags">
        ///   The tags to filter and select.
        /// </param>
        /// <param name="filter">
        ///   The filter to apply.
        /// </param>
        /// <returns>
        ///   The matching tags.
        /// </returns>
        public static IEnumerable<TagDefinition> ApplyFilter(this IEnumerable<TagDefinition> tags, FindTagsRequest? filter) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            if (filter == null) {
                return tags;
            }

            return tags.Where(x => x.MatchesFilter(filter)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).SelectPage(filter);
        }


        /// <summary>
        /// Creates a copy of the <see cref="TagDefinition"/>, optionally omitting some parts of 
        /// the original definition from the copy
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="fields">
        ///   The fields to include in the copy.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinition"/> instance that is populated using the original 
        ///   <paramref name="tag"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static TagDefinition Clone(this TagDefinition tag, TagDefinitionFields fields = TagDefinitionFields.All) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            if (fields == TagDefinitionFields.All) {
                return TagDefinition.FromExisting(tag);
            }

            var builder = TagDefinitionBuilder.CreateFromExisting(tag);
            if (!fields.HasFlag(TagDefinitionFields.DigitalStates)) {
                builder.ClearDigitalStates();
            }

            if (!fields.HasFlag(TagDefinitionFields.SupportedFeatures)) {
                builder.ClearSupportedFeatures();
            }

            if (!fields.HasFlag(TagDefinitionFields.Properties)) {
                builder.ClearProperties();
            }

            if (!fields.HasFlag(TagDefinitionFields.Labels)) {
                builder.ClearLabels();
            }

            return builder.Build();
        }

    }
}
