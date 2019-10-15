namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base implementation of <see cref="EventMessageSubscriptionBase"/>.
    /// </summary>
    public abstract class EventMessageSubscriptionBase : AdapterSubscription<EventMessage>, IEventMessageSubscription { }

}
