using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Default implementation of the <see cref="IConfigurationChanges"/> feature.
    /// </summary>
    public class ConfigurationChanges : SubscriptionManager<ConfigurationChangesOptions, string, ConfigurationChange, ConfigurationChangesSubscription>, IConfigurationChanges {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;


        /// <summary>
        /// Creates a new <see cref="ConfigurationChanges"/> object.
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
        public ConfigurationChanges(
            ConfigurationChangesOptions? options, 
            IBackgroundTaskService? backgroundTaskService, 
            ILogger? logger
        ) : base(options, backgroundTaskService, logger) { }


        /// <inheritdoc/>
        protected override ConfigurationChangesSubscription CreateSubscriptionChannel(
            IAdapterCallContext context, 
            int id, 
            int channelCapacity,
            CancellationToken[] cancellationTokens, 
            Func<ValueTask> cleanup,
            object? state
        ) {
            return new ConfigurationChangesSubscription(
                id,
                context,
                BackgroundTaskService,
                cancellationTokens,
                cleanup,
                channelCapacity
            );
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<ConfigurationChange> Subscribe(
            IAdapterCallContext context, 
            ConfigurationChangesSubscriptionRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            ValidationExtensions.ValidateObject(request);

            var subscription = await CreateSubscriptionAsync(
                context, 
                string.Concat(WellKnownFeatures.Diagnostics.ConfigurationChanges, nameof(Subscribe)), 
                null, 
                cancellationToken
            ).ConfigureAwait(false);

            if (request.ItemTypes != null && request.ItemTypes.Any()) {
                subscription.AddTopics(request.ItemTypes);
            }

            await foreach (var item in subscription.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <inheritdoc/>
        protected override ValueTask<bool> IsTopicMatch(ConfigurationChange value, IEnumerable<string> topics, CancellationToken cancellationToken) {
            var result = topics.Any(x => string.Equals(value.ItemType, x, StringComparison.OrdinalIgnoreCase));
            return new ValueTask<bool>(result);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _isDisposed = true;
        }

    }


    /// <summary>
    /// Options for <see cref="ConfigurationChanges"/>
    /// </summary>
    public class ConfigurationChangesOptions : SubscriptionManagerOptions { }


    /// <summary>
    /// Subscription type for <see cref="ConfigurationChanges"/>.
    /// </summary>
    public class ConfigurationChangesSubscription : SubscriptionChannel<string, ConfigurationChange> {

        /// <summary>
        /// Creates a new <see cref="ConfigurationChangesSubscription"/> object.
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
        public ConfigurationChangesSubscription(
            int id, 
            IAdapterCallContext context, 
            IBackgroundTaskService backgroundTaskService, 
            CancellationToken[] cancellationTokens, 
            Func<ValueTask> cleanup, 
            int capacity
        ) : base(id, context, backgroundTaskService, TimeSpan.Zero, cancellationTokens, cleanup, capacity) { }

    }

}
