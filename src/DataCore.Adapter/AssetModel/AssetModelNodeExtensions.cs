using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Extension methods for <see cref="AssetModelNode"/>
    /// </summary>
    public static class AssetModelNodeExtensions {

        /// <summary>
        /// Tests if the asset model node matches a search filter.
        /// </summary>
        /// <param name="node">
        ///   The node.
        /// </param>
        /// <param name="filter">
        ///   The asset model search filter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the node matches the filter, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="node"/> is <see langword="null"/>.
        /// </exception>
        public static bool MatchesFilter(this AssetModelNode node, FindAssetModelNodesRequest? filter) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }
            
            if (filter == null) {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(filter.Name)) {
                if (!node.Name.Like(filter.Name!)) {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Description)) {
                if (!node.Description.Like(filter.Description!)) {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Applies filter terms to the specified asset model nodes and selects a page of results.
        /// </summary>
        /// <param name="nodes">
        ///   The nodes to filter and select.
        /// </param>
        /// <param name="filter">
        ///   The filter to apply.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="nodes"/> is <see langword="null"/>.
        /// </exception>
        public static IEnumerable<AssetModelNode> ApplyFilter(this IEnumerable<AssetModelNode> nodes, FindAssetModelNodesRequest? filter) {
            if (nodes == null) {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (filter == null) {
                return nodes;
            }

            return nodes.Where(x => x.MatchesFilter(filter)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).SelectPage(filter);
        }


        /// <summary>
        /// Applies filter terms to the specified asset model nodes and selects a page of results.
        /// </summary>
        /// <param name="nodes">
        ///   The nodes to filter and select.
        /// </param>
        /// <param name="filter">
        ///   The filter to apply.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="nodes"/> is <see langword="null"/>.
        /// </exception>
        public static IEnumerable<AssetModelNode> ApplyFilter(this IEnumerable<AssetModelNode> nodes, BrowseAssetModelNodesRequest? filter) {
            if (nodes == null) {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (filter == null) {
                return nodes;
            }

            var result = filter.ParentId == null
                ? nodes.Where(x => x.Parent == null)
                : nodes.Where(x => filter.ParentId.Equals(x.Parent, StringComparison.OrdinalIgnoreCase));

            return nodes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).SelectPage(filter);
        }

    }

}
