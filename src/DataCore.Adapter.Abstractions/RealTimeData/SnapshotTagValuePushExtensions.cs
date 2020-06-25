using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extensions for <see cref="ISnapshotTagValuePush"/>.
    /// </summary>
    public static class SnapshotTagValuePushExtensions {

        /// <summary>
        /// Creates a snapshot tag value change subscription and adds the specified tags to the 
        /// subscription.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="ISnapshotTagValuePush"/> feature.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="tags">
        ///   The tags to add to the subscription.
        /// </param>
        /// <returns>
        ///   A task that will return the new <see cref="ISnapshotTagValueSubscription"/>.
        /// </returns>
        public static async Task<ISnapshotTagValueSubscription> Subscribe(
            this ISnapshotTagValuePush feature, 
            IAdapterCallContext context, 
            IEnumerable<string> tags
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            
            var result = await feature.Subscribe(context).ConfigureAwait(false);

            if (tags != null) {
                foreach (var tag in tags) {
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }

                    await result.AddTagToSubscription(tag).ConfigureAwait(false);
                }
            }

            return result;
        }


        /// <summary>
        /// Adds a tag to the subscription. Equivalent to calling 
        /// <see cref="IAdapterSubscriptionWithTopics{T}.SubscribeToTopic"/>.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        public static ValueTask<bool> AddTagToSubscription(this ISnapshotTagValueSubscription subscription, string tag) {
            if (subscription == null) {
                throw new ArgumentNullException(nameof(subscription));
            }
            return subscription.SubscribeToTopic(tag);
        }


        /// <summary>
        /// Removes a tag from the subscription. Equivalent to calling 
        /// <see cref="IAdapterSubscriptionWithTopics{T}.UnsubscribeFromTopic(string)"/>.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        public static ValueTask<bool> RemoveTagFromSubscription(this ISnapshotTagValueSubscription subscription, string tag) {
            if (subscription == null) {
                throw new ArgumentNullException(nameof(subscription));
            }
            return subscription.UnsubscribeFromTopic(tag);
        }

    }

}
