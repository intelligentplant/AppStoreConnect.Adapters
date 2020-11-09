using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Default implementation of the <see cref="ITagConfigurationChanges"/> feature.
    /// </summary>
    public class TagConfigurationChanges : SubscriptionManager<TagConfigurationChangesOptions, string, TagConfigurationChange, TagConfigurationChangesSubscription>, ITagConfigurationChanges {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationChanges"/> object.
        /// </summary>
        /// <param name="options">
        ///   The subscription manager options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </param>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        public TagConfigurationChanges(
            TagConfigurationChangesOptions? options, 
            IBackgroundTaskService? backgroundTaskService, 
            ILogger? logger
        ) : base(options, backgroundTaskService, logger) { }


        /// <inheritdoc/>
        protected override TagConfigurationChangesSubscription CreateSubscriptionChannel(
            IAdapterCallContext context, 
            int id, 
            int channelCapacity,
            CancellationToken[] cancellationTokens, 
            Action cleanup,
            object? state
        ) {
            return new TagConfigurationChangesSubscription(
                id,
                context,
                BackgroundTaskService,
                cancellationTokens,
                cleanup,
                channelCapacity
            );
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            TagConfigurationChangesSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            ValidationExtensions.ValidateObject(request);

            var subscription = CreateSubscription(context, null, cancellationToken);
            return Task.FromResult(subscription.Reader);
        }
    }


    /// <summary>
    /// Options for <see cref="TagConfigurationChanges"/>
    /// </summary>
    public class TagConfigurationChangesOptions : SubscriptionManagerOptions { }


    /// <summary>
    /// Subscription type for <see cref="TagConfigurationChanges"/>.
    /// </summary>
    public class TagConfigurationChangesSubscription : SubscriptionChannel<string, TagConfigurationChange> {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationChangesSubscription"/> object.
        /// </summary>
        /// <param name="id">
        ///   The subscription ID.
        /// </param>
        /// <param name="context">
        ///   The context for the subscriber.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The background task service, used to run publish operations in a background task if required.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A set of cancellation tokens that will be observed in order to detect 
        ///   cancellation of the subscription.
        /// </param>
        /// <param name="cleanup">
        ///   An action that will be invoked when the subscription is cancelled or disposed.
        /// </param>
        /// <param name="capacity">
        ///   The capacity of the output channel. A value less than or equal to zero specifies 
        ///   that an unbounded channel will be used. When a bounded channel is used, 
        ///   <see cref="BoundedChannelFullMode.DropWrite"/> is used as the behaviour when 
        ///   writing to a full channel.
        /// </param>
        public TagConfigurationChangesSubscription(
            int id, 
            IAdapterCallContext context, 
            IBackgroundTaskService backgroundTaskService, 
            CancellationToken[] cancellationTokens, 
            Action cleanup, 
            int capacity
        ) : base(id, context, backgroundTaskService, TimeSpan.Zero, cancellationTokens, cleanup, capacity) { }

    }

}
