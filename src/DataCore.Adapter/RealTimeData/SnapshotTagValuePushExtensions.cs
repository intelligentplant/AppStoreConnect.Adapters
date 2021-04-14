using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extensions for <see cref="ISnapshotTagValuePush"/>.
    /// </summary>
    public static class SnapshotTagValuePushExtensions {

        /// <summary>
        /// Creates a snapshot value change subscription that cannot be modified after creation.
        /// </summary>
        /// <param name="feature">
        ///   The feature.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request describing the subscription settings.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A channel reader that will emit tag values as they occur.
        /// </returns>
        public static IAsyncEnumerable<TagValueQueryResult> Subscribe(
            this ISnapshotTagValuePush feature,
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            var channel = Array.Empty<TagValueSubscriptionUpdate>().PublishToChannel();

            return feature.Subscribe(context, request, channel.ReadAllAsync(cancellationToken), cancellationToken);
        }

    }
}
