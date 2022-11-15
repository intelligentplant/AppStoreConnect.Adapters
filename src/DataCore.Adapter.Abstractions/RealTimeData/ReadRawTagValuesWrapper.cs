using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IReadRawTagValues"/>.
    /// </summary>
    internal class ReadRawTagValuesWrapper : AdapterFeatureWrapper<IReadRawTagValues>, IReadRawTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadRawTagValuesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadRawTagValuesWrapper(AdapterCore adapter, IReadRawTagValues innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<TagValueQueryResult> IReadRawTagValues.ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadRawTagValues, cancellationToken);
        }

    }

}
