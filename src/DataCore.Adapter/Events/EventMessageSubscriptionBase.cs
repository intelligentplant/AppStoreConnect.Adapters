using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base implementation of <see cref="IEventMessageSubscription"/>.
    /// </summary>
    public abstract class EventMessageSubscriptionBase : AdapterSubscription<EventMessage>, IEventMessageSubscription { 
    
        /// <summary>
        /// The subscription type.
        /// </summary>
        public EventMessageSubscriptionType SubscriptionType { get; }


        /// <summary>
        /// Creates a new <see cref="EventMessageSubscriptionBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        /// <param name="subscriptionType">
        ///   The event subscription type.
        /// </param>
        protected EventMessageSubscriptionBase(IAdapterCallContext context, string id, EventMessageSubscriptionType subscriptionType) 
            : base(context, id) {
            SubscriptionType = subscriptionType;
        }


        /// <inheritdoc/>
        protected sealed override async Task Run(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
            OnRunning();
            await RunSubscription(cancellationToken).ConfigureAwait(false);
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
        /// Starts a long-running task that will run the subscription until the provided 
        /// cancellation token fires.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the long-running task.
        /// </param>
        /// <returns>
        ///   A long-running task.
        /// </returns>
        protected virtual Task RunSubscription(CancellationToken cancellationToken) {
            return Completed;
        }


        /// <inheritdoc/>
        protected override void OnCancelled() {
            // Do nothing.
        }

    }

}
