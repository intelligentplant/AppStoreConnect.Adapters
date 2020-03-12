using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A subscription created by a call to <see cref="ISnapshotTagValuePush.Subscribe"/>.
    /// </summary>
    public abstract class SnapshotTagValueSubscriptionBase : AdapterSubscription<TagValueQueryResult>, ISnapshotTagValueSubscription {

        /// <summary>
        /// Channel that will publish changes to tag subscriptions.
        /// </summary>
        private readonly Channel<UpdateSnapshotTagValueSubscriptionRequest> _tagsChannel = Channel.CreateUnbounded<UpdateSnapshotTagValueSubscriptionRequest>();

        /// <summary>
        /// The subscribed tags.
        /// </summary>
        private readonly Dictionary<string, TagIdentifier> _subscribedTags = new Dictionary<string, TagIdentifier>(StringComparer.Ordinal);

        /// <summary>
        /// Lock for accessing <see cref="_subscribedTags"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscribedTagsLock = new ReaderWriterLockSlim();


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscriptionBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        protected SnapshotTagValueSubscriptionBase(IAdapterCallContext context)
            : base(context) { }


        /// <inheritdoc/>
        protected sealed override async Task Run(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
            await ProcessSubscriptionChangesChannel(_tagsChannel, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Starts a long-running task that will read subscription changes from the specified 
        /// channel until the channel closes or the provided cancellation token fires.
        /// </summary>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token.
        /// </param>
        /// <returns>
        ///   A long-running task that will run until the channel closes or the cancellation token 
        ///   fires.
        /// </returns>
        protected virtual async Task ProcessSubscriptionChangesChannel(
            ChannelReader<UpdateSnapshotTagValueSubscriptionRequest> channel,
            CancellationToken cancellationToken
        ) {
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!channel.TryRead(out var change) || change == null || string.IsNullOrWhiteSpace(change.Tag)) {
                    continue;
                }

                var tagInfo = await ResolveTag(Context, change.Tag, cancellationToken).ConfigureAwait(false);
                if (tagInfo == null) {
                    // Not a valid tag.
                    continue;
                }

                switch (change.Action) {
                    case Common.SubscriptionUpdateAction.Subscribe:
                        await AddTagToSubscription(tagInfo).ConfigureAwait(false);
                        break;
                    case Common.SubscriptionUpdateAction.Unsubscribe:
                        await RemoveTagFromSubscription(tagInfo).ConfigureAwait(false);
                        break;
                }
            }
        }


        /// <summary>
        /// Performs any required initialisation tasks when the subscription is started.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will perform any required initialisation tasks.
        /// </returns>
        protected virtual Task Init(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Gets all tags that have been added to the subscription.
        /// </summary>
        /// <returns>
        ///   A collection of <see cref="TagIdentifier"/> objects representing the subscribed 
        ///   tags.
        /// </returns>
        public IEnumerable<TagIdentifier> GetSubscribedTags() {
            _subscribedTagsLock.EnterReadLock();
            try {
                return _subscribedTags.Values.ToArray();
            }
            finally {
                _subscribedTagsLock.ExitReadLock();
            }
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
        /// Adds a tag to the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        public async ValueTask<bool> AddTagToSubscription(string tag) {
            if (string.IsNullOrWhiteSpace(tag) || !await _tagsChannel.Writer.WaitToWriteAsync(CancellationToken).ConfigureAwait(false)) {
                return false;
            }

            return _tagsChannel.Writer.TryWrite(new UpdateSnapshotTagValueSubscriptionRequest() { 
                Tag = tag,
                Action = Common.SubscriptionUpdateAction.Subscribe
            });
        }


        /// <summary>
        /// Adds a tag to the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag to add.
        /// </param>
        /// <returns>
        ///   A task that will add the tag to the subscription.
        /// </returns>
        private async Task AddTagToSubscription(TagIdentifier tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            _subscribedTagsLock.EnterUpgradeableReadLock();
            try {
                if (_subscribedTags.ContainsKey(tag.Id)) {
                    return;
                }
                _subscribedTagsLock.EnterWriteLock();
                try {
                    _subscribedTags[tag.Id] = tag;
                }
                finally {
                    _subscribedTagsLock.ExitWriteLock();
                }
            }
            finally {
                _subscribedTagsLock.ExitUpgradeableReadLock();
            }
            // Notify that the tag was added to the subscription.
            await OnTagAdded(tag).ConfigureAwait(false);
        }


        /// <summary>
        /// Removes a tag from the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        public async ValueTask<bool> RemoveTagFromSubscription(string tag) {
            if (string.IsNullOrWhiteSpace(tag) || !await _tagsChannel.Writer.WaitToWriteAsync(CancellationToken).ConfigureAwait(false)) {
                return false;
            }

            return _tagsChannel.Writer.TryWrite(new UpdateSnapshotTagValueSubscriptionRequest() {
                Tag = tag,
                Action = Common.SubscriptionUpdateAction.Unsubscribe
            });
        }


        /// <summary>
        /// Removes a tag from the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag to remove.
        /// </param>
        /// <returns>
        ///   A task that will remove the tag from the subscription.
        /// </returns>
        private async Task RemoveTagFromSubscription(TagIdentifier tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            var removed = false;
            _subscribedTagsLock.EnterWriteLock();
            try {
                removed = _subscribedTags.Remove(tag.Id);
            }
            finally {
                _subscribedTagsLock.ExitWriteLock();
            }

            if (removed) {
                // Notify that the tag was removed from the subscription.
                await OnTagRemoved(tag).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Tests if the subscription is subscribed to receive the specified tag value.
        /// </summary>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription is subscribed to receive the tag value, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public virtual bool IsSubscribed(TagValueQueryResult value) {
            if (value == null) {
                return false;
            }

            _subscribedTagsLock.EnterReadLock();
            try {
                return _subscribedTags.ContainsKey(value.TagId);
            }
            finally {
                _subscribedTagsLock.ExitReadLock();
            }
        }


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


        /// <inheritdoc/>
        protected override void OnCancelled() {
            _tagsChannel.Writer.TryComplete();
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _subscribedTagsLock.EnterWriteLock();
            try {
                _subscribedTags.Clear();
            }
            finally {
                _subscribedTagsLock.ExitWriteLock();
                _subscribedTagsLock.Dispose();
            }
        }

    }
}
