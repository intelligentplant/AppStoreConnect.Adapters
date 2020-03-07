using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A subscription created by a call to <see cref="ISnapshotTagValuePush.Subscribe"/>.
    /// </summary>
    public abstract class SnapshotTagValueSubscription : AdapterSubscription<TagValueQueryResult>, ISnapshotTagValueSubscription {

        /// <summary>
        /// Channel that will publish changes to tag subscriptions.
        /// </summary>
        private readonly Channel<UpdateSnapshotTagValueSubscriptionRequest> _tagsChannel = Channel.CreateUnbounded<UpdateSnapshotTagValueSubscriptionRequest>();

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscription"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        protected SnapshotTagValueSubscription(IAdapterCallContext context)
            : base(context) { }


        /// <inheritdoc/>
        protected override Task Run(CancellationToken cancellationToken) {
            return ProcessTagsChannel(_tagsChannel, cancellationToken);
        }


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
        protected abstract Task ProcessTagsChannel(
            ChannelReader<UpdateSnapshotTagValueSubscriptionRequest> channel, 
            CancellationToken cancellationToken
        );


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _tagsChannel.Writer.TryComplete();
        }

    }
}
