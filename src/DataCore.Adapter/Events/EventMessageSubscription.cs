namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base implementation of <see cref="IEventMessageSubscription"/>.
    /// </summary>
    public abstract class EventMessageSubscription : AdapterSubscription<EventMessage>, IEventMessageSubscription { }

}
