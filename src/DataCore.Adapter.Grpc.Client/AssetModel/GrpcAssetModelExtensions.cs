using System.Linq;
using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents.
    /// </summary>
    public static class GrpcAssetModelExtensions {

        /// <summary>
        /// Converts a gRPC asset model node to its adapter equivalent.
        /// </summary>
        /// <param name="node">
        ///   The gRPC asset model node.
        /// </param>
        /// <returns>
        ///   The adapter asset model node.
        /// </returns>
        public static Models.AssetModelNode ToAdapterAssetModelNode(this AssetModelNode node) {
            if (node == null) {
                return null;
            }

            return Models.AssetModelNode.Create(
                node.Id,
                node.Name,
                node.Description,
                string.IsNullOrWhiteSpace(node.Parent) 
                    ? null 
                    : node.Parent,
                node.Children,
                node.Measurements.Select(x => Models.AssetModelNodeMeasurement.Create(x.Name, x.AdapterId, RealTimeData.Models.TagSummary.Create(x.Tag.Id, x.Tag.Name, x.Tag.Description, x.Tag.Units))).ToArray(),
                node.Properties
            );
        }

    }
}
