using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Default implementation of the <see cref="IEventMessagePush"/> feature.
    /// </summary>
    /// <remarks>
    ///   This implementation pushes ephemeral event messages to subscribers. To maintain an 
    ///   in-memory buffer of historical events, use <see cref="InMemoryEventMessageStore"/>.
    /// </remarks>
    public class EventMessagePush : SubscriptionManager<EventMessagePushOptions, string, EventMessage, EventSubscriptionChannel>, IEventMessagePush {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }


        /// <summary>
        /// Creates a new <see cref="EventMessagePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        public EventMessagePush(EventMessagePushOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger) 
            : base(options, backgroundTaskService, logger) { }


        /// <inheritdoc/>
        protected override EventSubscriptionChannel CreateSubscriptionChannel(
            IAdapterCallContext context, 
            int id, 
            int channelCapacity,
            CancellationToken[] cancellationTokens, 
            Func<ValueTask> cleanup, 
            object? state
        ) {
            var request = (CreateEventMessageSubscriptionRequest) state!;
            return new EventSubscriptionChannel(
                id, 
                context, 
                BackgroundTaskService, 
                request?.SubscriptionType ?? EventMessageSubscriptionType.Active, 
                TimeSpan.Zero, 
                cancellationTokens, 
                cleanup, 
                channelCapacity
            );
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<EventMessage> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request,
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

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposedToken)) {
                var subscription = await CreateSubscriptionAsync<IEventMessagePush>(context, nameof(Subscribe), request, ctSource.Token).ConfigureAwait(false);
                await foreach(var item in subscription.ReadAllAsync(ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask OnSubscriptionAddedAsync(EventSubscriptionChannel subscription, CancellationToken cancellationToken) {
            HasActiveSubscriptions = HasSubscriptions && GetSubscriptions().Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
            await base.OnSubscriptionAddedAsync(subscription, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask OnSubscriptionCancelledAsync(EventSubscriptionChannel subscription, CancellationToken cancellationToken) { 
            HasActiveSubscriptions = HasSubscriptions && GetSubscriptions().Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
            await base.OnSubscriptionCancelledAsync(subscription, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override IDictionary<string, string> GetHealthCheckProperties(IAdapterCallContext context) {
            var result = base.GetHealthCheckProperties(context);

            var subscriptions = GetSubscriptions();

            result[Resources.HealthChecks_Data_ActiveSubscriberCount] = subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Active).ToString(context?.CultureInfo);
            result[Resources.HealthChecks_Data_PassiveSubscriberCount] = subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Passive).ToString(context?.CultureInfo);

            return result;
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _isDisposed = true;
        }

    }


    /// <summary>
    /// Options for <see cref="EventMessagePush"/>
    /// </summary>
    public class EventMessagePushOptions : SubscriptionManagerOptions { }
}
