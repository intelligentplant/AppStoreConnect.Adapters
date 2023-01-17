using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IReadTagValuesAtTimes"/>.
    /// </summary>
    internal class ReadTagValuesAtTimesWrapper : AdapterFeatureWrapper<IReadTagValuesAtTimes>, IReadTagValuesAtTimes {

        /// <summary>
        /// Creates a new <see cref="ReadTagValuesAtTimesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadTagValuesAtTimesWrapper(AdapterCore adapter, IReadTagValuesAtTimes innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<TagValueQueryResult> IReadTagValuesAtTimes.ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadTagValuesAtTimes, cancellationToken);
        }

    }

}
