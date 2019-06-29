using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DataCore.Adapter.Grpc.Proxy.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.AssetModel {

    /// <summary>
    /// Extension methods for converting from gRPC asset model types to their adapter equivalents.
    /// </summary>
    internal static class AssetModelExtensions {

        internal static Adapter.AssetModel.Models.AssetModelNode ToAdapterAssetModelNode(this AssetModelNode node) {
            if (node == null) {
                return null;
            }

            return new Adapter.AssetModel.Models.AssetModelNode(
                node.Id,
                node.Name,
                node.Description,
                node.Parent,
                node.Children,
                node.Measurements.Select(x => new Adapter.AssetModel.Models.AssetModelNodeMeasurement(x.Name, x.AdapterId, new Adapter.RealTimeData.Models.TagIdentifier(x.Tag.Id, x.Tag.Name))).ToArray(),
                node.Properties
            );
        }

    }
}
