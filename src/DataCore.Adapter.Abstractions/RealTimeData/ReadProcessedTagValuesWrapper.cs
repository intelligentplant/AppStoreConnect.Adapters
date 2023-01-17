using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IReadProcessedTagValues"/>.
    /// </summary>
    internal class ReadProcessedTagValuesWrapper : AdapterFeatureWrapper<IReadProcessedTagValues>, IReadProcessedTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadProcessedTagValuesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadProcessedTagValuesWrapper(AdapterCore adapter, IReadProcessedTagValues innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<DataFunctionDescriptor> IReadProcessedTagValues.GetSupportedDataFunctions(IAdapterCallContext context, GetSupportedDataFunctionsRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.GetSupportedDataFunctions, cancellationToken);
        }


        /// <inheritdoc/>
        IAsyncEnumerable<ProcessedTagValueQueryResult> IReadProcessedTagValues.ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadProcessedTagValues, cancellationToken);
        }

    }

}
