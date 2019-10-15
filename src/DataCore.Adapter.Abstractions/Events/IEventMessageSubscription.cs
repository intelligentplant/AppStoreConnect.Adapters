namespace DataCore.Adapter.Events {

    /// <summary>
    /// Defines a subscription for receiving event messages as push notifications.
    /// </summary>
    public interface IEventMessageSubscription : IAdapterSubscription<EventMessage> { }

}
