namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base implementation of <see cref="IEventMessageSubscription"/>.
    /// </summary>
    public abstract class EventMessageSubscriptionBase : AdapterSubscription<EventMessage>, IEventMessageSubscription { 
    
        /// <summary>
        /// Creates a new <see cref="EventMessageSubscriptionBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        protected EventMessageSubscriptionBase(IAdapterCallContext context) 
            : base(context) { }

    }

}
