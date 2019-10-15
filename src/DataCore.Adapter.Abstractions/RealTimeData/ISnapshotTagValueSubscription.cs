using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a subscription for receiving snapshot tag values via push.
    /// </summary>
    public interface ISnapshotTagValueSubscription : IAdapterSubscription<TagValueQueryResult> {

        /// <summary>
        /// Gets the total number of tags that the subscription is observing.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the tags that the subscription is observing.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that returns the tag identifiers for the subscription's tags.
        /// </returns>
        ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds additional tags to the subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="tagNamesOrIds">
        ///   The names or IDs of the tags to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The total number of tags held by the subscription following the update.
        /// </returns>
        Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);

        /// <summary>
        /// Removes tags from the subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="tagNamesOrIds">
        ///   The names or IDs of the tags to unsubscribe from.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The total number of tags held by the subscription following the update.
        /// </returns>
        Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);

    }
}
