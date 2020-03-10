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
        /// <param name="subscriptionType">
        ///   The event subscription type.
        /// </param>
        protected EventMessageSubscriptionBase(IAdapterCallContext context, EventMessageSubscriptionType subscriptionType) 
            : base(context) {
            SubscriptionType = subscriptionType;
        }

    }

}
