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
        private readonly SnapshotTagValueStreamOptions _options;

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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public SnapshotTagValueSubscription(
            IAdapterCallContext context,
            SnapshotTagValueStreamOptions options
        ) : base(context) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
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

                var tagInfo = _options.TagResolver == null
                    ? new TagIdentifier(change.Tag, change.Tag)
                    : await _options.TagResolver.Invoke(Context, change.Tag, cancellationToken).ConfigureAwait(false);

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
                        if (_options.OnTagAdded != null) {
                            await _options.OnTagAdded.Invoke(this, tagInfo).ConfigureAwait(false);
                        }
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
                        if (removed && _options.OnTagRemoved != null) {
                            // Notify that the tag was removed from the subscription.
                            await _options.OnTagRemoved(this, tagInfo).ConfigureAwait(false);
                        }
                        break;
                }
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            
            if (disposing) {
                _options?.OnCancelled(this);
                lock (_subscribedTags) {
                    _subscribedTags.Clear();
                }
            }
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValueSubscriptionBase"/>.
    /// </summary>
    public class SnapshotTagValueStreamOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/> for the tag..
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
