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

            return new Models.AssetModelNode(
                node.Id,
                node.Name,
                node.Description,
                node.Parent,
                node.Children,
                node.Measurements.Select(x => new Models.AssetModelNodeMeasurement(x.Name, x.AdapterId, new Adapter.RealTimeData.Models.TagIdentifier(x.Tag.Id, x.Tag.Name))).ToArray(),
                node.Properties
            );
        }

    }
}
