using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.AssetModel.Models {

    /// <summary>
    /// Describes a measurement on an asset model node. Note that the measurement can be provided 
    /// from a tag on a different adapter to the one that the node is defined on.
    /// </summary>
    public class AssetModelNodeMeasurement {

        /// <summary>
        /// The measurement name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The ID of the adapter that the tag for the measurement is defined on.
        /// </summary>
        public string AdapterId { get; }

        /// <summary>
        /// The tag identifier for the measurement.
        /// </summary>
        public TagIdentifier Tag { get; }


        /// <summary>
        /// Creates a new <see cref="AssetModelNodeMeasurement"/> object.
        /// </summary>
        /// <param name="name">
        ///   The measurement name.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter that the tag for the measurement is defined on.
        /// </param>
        /// <param name="tag">
        ///   The tag identifier for the measurement.
        /// </param>
        public AssetModelNodeMeasurement(string name, string adapterId, TagIdentifier tag) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
        }

    }
}
