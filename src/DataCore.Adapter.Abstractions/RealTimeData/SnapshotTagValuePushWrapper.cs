using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="ISnapshotTagValuePush"/>.
    /// </summary>
    internal class SnapshotTagValuePushWrapper : AdapterFeatureWrapper<ISnapshotTagValuePush>, ISnapshotTagValuePush {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal SnapshotTagValuePushWrapper(AdapterCore adapter, ISnapshotTagValuePush innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<TagValueQueryResult> ISnapshotTagValuePush.Subscribe(IAdapterCallContext context, CreateSnapshotTagValueSubscriptionRequest request, IAsyncEnumerable<TagValueSubscriptionUpdate> subscriptionUpdates, CancellationToken cancellationToken) {
            return DuplexStreamAsync(context, request, subscriptionUpdates, InnerFeature.Subscribe, cancellationToken);
        }

    }

}
