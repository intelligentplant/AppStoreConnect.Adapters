using System.Collections.Generic;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Describes the values immediately after the end boundary for a <see cref="TagValueBucket"/>.
    /// </summary>
    public class PostBoundaryInfo {

        ///<summary>
        /// The best-quality value after the boundary.
        /// </summary>
        /// <remarks>
        ///   The <see cref="BestQualityValue"/> and <see cref="ClosestValue"/> properties will be 
        ///   different if a sample with a lower quality than <see cref="BestQualityValue"/> 
        ///   exists between boundary timestamp and <see cref="BestQualityValue"/>.
        /// </remarks>
        /// <seealso cref="ClosestValue"/>
        public TagValueExtended? BestQualityValue { get; private set; }

        /// <summary>
        /// The value immediately after the boundary, regardless of quality.
        /// </summary>
        /// <remarks>
        ///   The <see cref="BestQualityValue"/> and <see cref="ClosestValue"/> properties will be 
        ///   different if a sample with a lower quality than <see cref="BestQualityValue"/> 
        ///   exists between boundary timestamp and <see cref="BestQualityValue"/>.
        /// </remarks>
        /// <seealso cref="BestQualityValue"/>
        public TagValueExtended? ClosestValue { get; private set; }

        /// <summary>
        /// The status of the boundary. The value will be <see cref="TagValueStatus.Uncertain"/> 
        /// if <see cref="BestQualityValue"/> is <see langword="null"/> or <see cref="BestQualityValue"/> 
        /// and <see cref="ClosestValue"/> differ, and <see cref="TagValueStatus.Good"/> otherwise.
        /// </summary>
        public TagValueStatus BoundaryStatus {
            get {
                return BestQualityValue == null || BestQualityValue != ClosestValue
                    ? TagValueStatus.Uncertain
                    : TagValueStatus.Good;
            }
        }


        /// <summary>
        /// Updates the boundary value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        internal void UpdateValue(TagValueExtended value) {
            if (BestQualityValue == null) {
                // No boundary value set; update both BestQualityValue and ClosestValue.
                BestQualityValue = value;
                ClosestValue = value;
                return;
            }

            if (value.UtcSampleTime >= BestQualityValue.UtcSampleTime) {
                // Newer than current boundary value; we can dismiss it.
                return;
            }

            if (value.Status >= BestQualityValue.Status) {
                // Status is at least as good as the current boundary value; update both 
                // BestQualityValue and ClosestValue.
                BestQualityValue = value;
                ClosestValue = value;
                return;
            }

            // Status is worse than current boundary value; only update ClosestValue.
            ClosestValue = value;
        }


        /// <summary>
        /// Gets a collection of values that form the post-boundary region. Up to two samples will 
        /// be emitted.
        /// </summary>
        /// <returns>
        ///   The post-boundary values. Possible outputs are zero values, one value (if the 
        ///   <see cref="BestQualityValue"/> is also the <see cref="ClosestValue"/>), or two 
        ///   values (if the <see cref="BestQualityValue"/> and <see cref="ClosestValue"/> are 
        ///   different).
        /// </returns>
        internal IEnumerable<TagValueExtended> GetBoundarySamples() {
            if (BestQualityValue == null) {
                // No boundary values.
                yield break;
            }

            if (BestQualityValue != null) {
                yield return BestQualityValue;
            }

            if (ClosestValue != null && ClosestValue != BestQualityValue) {
                yield return ClosestValue;
            }
        }

    }
}
