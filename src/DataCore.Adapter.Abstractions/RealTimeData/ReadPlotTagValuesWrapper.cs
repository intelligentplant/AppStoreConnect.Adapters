using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IReadPlotTagValues"/>.
    /// </summary>
    internal class ReadPlotTagValuesWrapper : AdapterFeatureWrapper<IReadPlotTagValues>, IReadPlotTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadPlotTagValuesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadPlotTagValuesWrapper(AdapterCore adapter, IReadPlotTagValues innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<TagValueQueryResult> IReadPlotTagValues.ReadPlotTagValues(IAdapterCallContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadPlotTagValues, cancellationToken);
        }

    }

}
