using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A subscription created by a call to <see cref="ISnapshotTagValuePush.Subscribe"/>.
    /// </summary>
    public abstract class SnapshotTagValueSubscriptionBase : AdapterSubscriptionWithTopics<TagValueQueryResult, TagIdentifier>, ISnapshotTagValueSubscription {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscriptionBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        protected SnapshotTagValueSubscriptionBase(IAdapterCallContext context, string id)
            : base(context, id) { }


        /// <inheritdoc/>
        protected sealed override ValueTask<TagIdentifier> ResolveTopic(IAdapterCallContext context, string topic) {
            return ResolveTag(context, topic, CancellationToken);
        }


        /// <inheritdoc/>
        protected sealed override Task OnTopicAdded(TagIdentifier topic) {
            return OnTagAdded(topic);
        }


        /// <inheritdoc/>
        protected sealed override Task OnTopicRemoved(TagIdentifier topic) {
            return OnTagRemoved(topic);
        }


        /// <inheritdoc/>
        protected override string GetTopicNameForValue(TagValueQueryResult value) {
            return value?.TagId;
        }


        /// <inheritdoc/>
        public override bool IsSubscribed(TagValueQueryResult value) {
            if (base.IsSubscribed(value)) {
                // We are subscribed via the tag ID.
                return true;
            }

            // Check if we are subscribed via the tag name instead.
            return IsSubscribedToTopicName(value?.TagName);
        }


        /// <summary>
        /// Invoked when a tag name or ID must be resolved.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="tag">
        ///   The tag name or ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the resolved tag identifier, or 
        ///   <see langword="null"/> if the tag cannot be resolved.
        /// </returns>
        protected abstract ValueTask<TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken);


        /// <summary>
        /// Invoked when a tag is added to the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag that was added.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        protected abstract Task OnTagAdded(TagIdentifier tag);


        /// <summary>
        /// Invoked when a tag is removed from the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag that was removed.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        protected abstract Task OnTagRemoved(TagIdentifier tag);

    }
}
