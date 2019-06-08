using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.RealTimeData.Models {

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

            if (!String.IsNullOrWhiteSpace(filter.Name)) {
                if (!tag.Name.Like(filter.Name)) {
                    return false;
                }
            }

            if (!String.IsNullOrWhiteSpace(filter.Description)) {
                if (!tag.Description.Like(filter.Description)) {
                    return false;
                }
            }

            if (!String.IsNullOrWhiteSpace(filter.Units)) {
                if (!tag.Units.Like(filter.Units)) {
                    return false;
                }
            }

            foreach (var item in filter.Other) {
                if (String.IsNullOrWhiteSpace(item.Value)) {
                    continue;
                }

                if (!tag.Properties.TryGetValue(item.Key, out var propertyValue)) {
                    return false;
                }

                if (!propertyValue.Like(item.Value)) {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Selects a page of tag definitions based on the paging settings in a search filter. The 
        /// tags are assumed to match the term filters in the filter object.
        /// </summary>
        /// <param name="tags">
        ///   The tags to select from.
        /// </param>
        /// <param name="filter">
        ///   The search filter.
        /// </param>
        /// <returns>
        ///   The selected tags. Tags will be ordered by name before applying the paging.
        /// </returns>
        public static IEnumerable<TagDefinition> SelectPage(this IEnumerable<TagDefinition> tags, FindTagsRequest filter) {
            if (tags == null) {
                return null;
            }

            if (filter == null) {
                return tags;
            }

            return tags.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Skip(filter.PageSize * (filter.Page - 1)).Take(filter.PageSize);
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
        public static IEnumerable<TagDefinition> ApplyFilter(this IEnumerable<TagDefinition> tags, FindTagsRequest filter) {
            if (tags == null) {
                return null;
            }

            if (filter == null) {
                return tags;
            }

            return tags.Where(x => x.MatchesFilter(filter)).SelectPage(filter);
        }


        /// <summary>
        /// Gets the text value for a tag based on its numeric value.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="numericValue">
        ///   The numeric value.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use.
        /// </param>
        /// <returns>
        ///   The equivalent text value. For state-based tags, this will be the name of the state that 
        ///   matches the numeric value.
        /// </returns>
        public static string GetTextValue(this TagDefinition tag, double numericValue, IFormatProvider provider = null) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            switch (tag.DataType) {
                case TagDataType.State:
                    return tag.States.FirstOrDefault(x => x.Value == numericValue).Key ?? TagValueBuilder.GetTextValue(numericValue, provider);
                default:
                    return TagValueBuilder.GetTextValue(numericValue, provider);
            }
        }


        /// <summary>
        /// Gets the numeric value for a tag based on its text value.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="textValue">
        ///   The text value.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use when parsing the text value.
        /// </param>
        /// <returns>
        ///   The equivalent numeric value. For state-based tags, this will be the value of the state 
        ///   that matches the text value.
        /// </returns>
        public static double GetNumericValue(this TagDefinition tag, string textValue, IFormatProvider provider = null) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            double num;

            switch (tag.DataType) {
                case TagDataType.State:
                    var state = tag.States.FirstOrDefault(x => x.Key.Equals(textValue, StringComparison.OrdinalIgnoreCase));
                    return state.Key != null
                        ? state.Value
                        : double.TryParse(textValue, NumberStyles.Float | NumberStyles.AllowThousands, provider ?? CultureInfo.CurrentCulture, out num)
                            ? num
                            : double.NaN;
                default:
                    return double.TryParse(textValue, NumberStyles.Float | NumberStyles.AllowThousands, provider ?? CultureInfo.CurrentCulture, out num)
                            ? num
                            : double.NaN;
            }
        }

    }
}
