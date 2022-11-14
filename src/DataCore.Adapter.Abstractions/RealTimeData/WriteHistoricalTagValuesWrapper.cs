using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IWriteHistoricalTagValues"/>.
    /// </summary>
    internal class WriteHistoricalTagValuesWrapper : AdapterFeatureWrapper<IWriteHistoricalTagValues>, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal WriteHistoricalTagValuesWrapper(AdapterCore adapter, IWriteHistoricalTagValues innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<WriteTagValueResult> IWriteHistoricalTagValues.WriteHistoricalTagValues(IAdapterCallContext context, WriteTagValuesRequest request, IAsyncEnumerable<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            return DuplexStreamAsync(context, request, channel, InnerFeature.WriteHistoricalTagValues, cancellationToken);
        }

    }

}
