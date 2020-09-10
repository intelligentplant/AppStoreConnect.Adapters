using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.RealTimeData {
    /// <summary>
    /// Defines a subscription channel for a tag value subscription.
    /// </summary>
    /// <typeparam name="TIdentifier">
    ///   The subscription ID type.
    /// </typeparam>
    public class TagValueSubscriptionChannel<TIdentifier> : SubscriptionChannel<TIdentifier, TagIdentifier, TagValueQueryResult> {

        /// <summary>
        /// Creates a new <see cref="TagValueSubscriptionChannel{TIdentifier}"/> object.
        /// </summary>
        /// <param name="id">
        ///   The subscription ID.
        /// </param>
        /// <param name="context">
        ///   The context for the subscriber.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/>, used to run publish operations in a 
        ///   background task if required.
        /// </param>
        /// <param name="tags">
        ///   The tags to subscribe to.
        /// </param>
        /// <param name="publishInterval">
        ///   The publish interval for the subscription. When greater than <see cref="TimeSpan.Zero"/>, 
        ///   a background task will be used to periodically publish the last-received message. 
        ///   Otherwise, messages will be published immediately.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A set of cancellation tokens that will be observed in order to detect 
        ///   cancellation of the subscription.
        /// </param>
        /// <param name="cleanup">
        ///   An action that will be invoked when the subscription is cancelled or disposed.
        /// </param>
        /// <param name="channelCapacity">
        ///   The capacity of the output channel. A value less than or equal to zero specifies 
        ///   that an unbounded channel will be used. When a bounded channel is used, 
        ///   <see cref="BoundedChannelFullMode.DropWrite"/> is used as the behaviour when 
        ///   writing to a full channel.
        /// </param>
        public TagValueSubscriptionChannel(
            TIdentifier id,
            IAdapterCallContext context,
            IBackgroundTaskService backgroundTaskService,
            TimeSpan publishInterval,
            CancellationToken[] cancellationTokens,
            Action cleanup,
            int channelCapacity = 0
        ) : base(
            id,
            context,
            backgroundTaskService,
            publishInterval,
            cancellationTokens,
            cleanup,
            channelCapacity
        ) { }

    }
}
