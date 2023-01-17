using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IWriteSnapshotTagValues"/>.
    /// </summary>
    internal class WriteSnapshotTagValuesWrapper : AdapterFeatureWrapper<IWriteSnapshotTagValues>, IWriteSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteSnapshotTagValuesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal WriteSnapshotTagValuesWrapper(AdapterCore adapter, IWriteSnapshotTagValues innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<WriteTagValueResult> IWriteSnapshotTagValues.WriteSnapshotTagValues(IAdapterCallContext context, WriteTagValuesRequest request, IAsyncEnumerable<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            return DuplexStreamAsync(context, request, channel, InnerFeature.WriteSnapshotTagValues, cancellationToken);
        }

    }

}
