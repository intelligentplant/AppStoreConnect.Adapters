using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Represents a single subscriber to a <see cref="SnapshotTagValuePush"/>.
    /// </summary>
    public class SnapshotTagValueSubscription : SnapshotTagValueSubscriptionBase {

        /// <summary>
        /// The stream options.
        /// </summary>
        private readonly SnapshotTagValueSubscriptionOptions _options;

        /// <summary>
        /// The list of subscribed tags.
        /// </summary>
        private readonly List<TagIdentifier> _subscribedTags = new List<TagIdentifier>();


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscriptionBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The adapter call context for the subscription.
        /// </param>
        /// <param name="options">
        ///   Additional options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        public SnapshotTagValueSubscription(
            IAdapterCallContext context,
            SnapshotTagValueSubscriptionOptions options
        ) : base(context) {
            _options = options;
        }


        /// <summary>
        /// Gets all tags that have been added to the subscription.
        /// </summary>
        /// <returns>
        ///   A collection of <see cref="TagIdentifier"/> objects representing the subscribed 
        ///   tags.
        /// </returns>
        public IEnumerable<TagIdentifier> GetSubscribedTags() {
            lock (_subscribedTags) {
                return _subscribedTags.ToArray();
            }
        }


        /// <inheritdoc/>
        protected override async Task ProcessTagsChannel(
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
                        lock (_subscribedTags) {
                            _subscribedTags.Add(tagInfo);
                        }
                        // Notify that the tag was added to the subscription.
                        await OnTagAdded(tagInfo).ConfigureAwait(false);
                        break;
                    case Common.SubscriptionUpdateAction.Unsubscribe:
                        var removed = false;
                        lock (_subscribedTags) {
                            var toBeRemoved = _subscribedTags.FindIndex(ti => TagIdentifierComparer.Id.Equals(tagInfo, ti));
                            if (toBeRemoved >= 0) {
                                _subscribedTags.RemoveAt(toBeRemoved);
                                removed = true;
                            }
                        }
                        if (removed) {
                            // Notify that the tag was removed from the subscription.
                            await OnTagRemoved(tagInfo).ConfigureAwait(false);
                        }
                        break;
                }
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
        /// <remarks>
        ///   The default implementation of this method uses <see cref="SnapshotTagValueSubscriptionOptions.TagResolver"/> 
        ///   on the options supplied to the constructor to resolve tags.
        /// </remarks>
        protected virtual async ValueTask<TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken) {
            return _options?.TagResolver == null
                ? new TagIdentifier(tag, tag)
                : await _options.TagResolver.Invoke(context, tag, cancellationToken).ConfigureAwait(false);

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
        /// <remarks>
        ///   The default implementation of this method invokes <see cref="SnapshotTagValueSubscriptionOptions.OnTagAdded"/> 
        ///   on the options supplied to the constructor.
        /// </remarks>
        protected virtual async Task OnTagAdded(TagIdentifier tag) {
            if (_options?.OnTagAdded != null) {
                await _options.OnTagAdded.Invoke(this, tag).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Invoked when a tag is removed from the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag that was removed.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        /// <remarks>
        ///   The default implementation of this method invokes <see cref="SnapshotTagValueSubscriptionOptions.OnTagRemoved"/> 
        ///   on the options supplied to the constructor.
        /// </remarks>
        protected virtual async Task OnTagRemoved(TagIdentifier tag) {
            if (_options?.OnTagRemoved != null) {
                await _options.OnTagRemoved.Invoke(this, tag).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Invoked when the subscription is cancelled.
        /// </summary>
        /// <remarks>
        /// <remarks>
        ///   The default implementation of this method invokes <see cref="SnapshotTagValueSubscriptionOptions.OnCancelled"/> 
        ///   on the options supplied to the constructor.
        /// </remarks>
        protected virtual void OnCancelled() {
            _options?.OnCancelled(this);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            
            if (disposing) {
                OnCancelled();
                lock (_subscribedTags) {
                    _subscribedTags.Clear();
                }
            }
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValueSubscription"/>.
    /// </summary>
    public class SnapshotTagValueSubscriptionOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/> for the tag.
        /// </summary>
        public Func<IAdapterCallContext, string, CancellationToken, ValueTask<TagIdentifier>> TagResolver { get; set; }

        /// <summary>
        /// A delegate that is invoked when a tag is added to the subscription.
        /// </summary>
        public Func<SnapshotTagValueSubscription, TagIdentifier, Task> OnTagAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when a tag is removed from the subscription.
        /// </summary>
        public Func<SnapshotTagValueSubscription, TagIdentifier, Task> OnTagRemoved { get; set; }

        /// <summary>
        /// A delegate that is invoked when the subscription is cancelled.
        /// </summary>
        public Action<SnapshotTagValueSubscription> OnCancelled { get; set; }

    }

}
