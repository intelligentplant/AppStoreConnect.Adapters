using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a subscription for receiving snapshot tag values via push.
    /// </summary>
    public interface ISnapshotTagValueSubscription : IDisposable {

        /// <summary>
        /// Gets the identifiers for the tags that the subscription is observing.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A collection of subscribed tag identifiers.
        /// </returns>
        Task<IEnumerable<TagIdentifier>> GetSubscribedTags(CancellationToken cancellationToken);

        /// <summary>
        /// Adds tags to the subscription.
        /// </summary>
        /// <param name="tagNamesOrIds">
        ///   The tag names or IDs to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The total number of subscriptions held after the update.
        /// </returns>
        Task<int> AddTagsToSubscription(IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);

        /// <summary>
        /// Removes tags from the subscription.
        /// </summary>
        /// <param name="tagNamesOrIds">
        ///   The tag names or IDs to unsubscribe from.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The total number of subscriptions held after the update.
        /// </returns>
        Task<int> RemoveTagsFromSubscription(IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);

    }
}
