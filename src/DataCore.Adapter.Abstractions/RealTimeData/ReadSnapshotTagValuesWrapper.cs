using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IReadSnapshotTagValues"/>.
    /// </summary>
    internal class ReadSnapshotTagValuesWrapper : AdapterFeatureWrapper<IReadSnapshotTagValues>, IReadSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadSnapshotTagValuesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadSnapshotTagValuesWrapper(AdapterCore adapter, IReadSnapshotTagValues innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadSnapshotTagValues, cancellationToken);
        }

    }

}
