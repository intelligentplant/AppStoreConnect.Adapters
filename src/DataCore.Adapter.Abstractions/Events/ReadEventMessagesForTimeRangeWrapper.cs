using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Wrapper for <see cref="IReadEventMessagesForTimeRange"/>.
    /// </summary>
    internal class ReadEventMessagesForTimeRangeWrapper : AdapterFeatureWrapper<IReadEventMessagesForTimeRange>, IReadEventMessagesForTimeRange {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesForTimeRangeWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadEventMessagesForTimeRangeWrapper(AdapterCore adapter, IReadEventMessagesForTimeRange innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<EventMessage> IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadEventMessagesForTimeRange, cancellationToken);
        }

    }

}
