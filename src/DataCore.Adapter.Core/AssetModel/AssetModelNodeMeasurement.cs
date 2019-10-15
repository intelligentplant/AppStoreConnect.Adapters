using System;
using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a measurement on an asset model node. Note that the measurement can be provided 
    /// from a tag on a different adapter to the one that the node is defined on.
    /// </summary>
    public class AssetModelNodeMeasurement {

        /// <summary>
        /// The measurement name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The ID of the adapter that the tag for the measurement is defined on.
        /// </summary>
        [Required]
        public string AdapterId { get; set; }

        /// <summary>
        /// The tag summary for the measurement.
        /// </summary>
        [Required]
        public TagSummary Tag { get; set; }


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
        ///   The tag summary for the measurement.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static AssetModelNodeMeasurement Create(string name, string adapterId, TagSummary tag) {
            return new AssetModelNodeMeasurement() {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId)),
                Tag = tag ?? throw new ArgumentNullException(nameof(tag))
            };
        }

    }
}
