namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base implementation of <see cref="IEventMessageSubscription"/>.
    /// </summary>
    public abstract class EventMessageSubscription : AdapterSubscription<EventMessage>, IEventMessageSubscription { 
    
        /// <summary>
        /// Creates a new <see cref="EventMessageSubscription"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        protected EventMessageSubscription(IAdapterCallContext context) 
            : base(context) { }

    }

}
