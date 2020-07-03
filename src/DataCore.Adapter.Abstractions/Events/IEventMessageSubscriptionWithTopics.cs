
namespace DataCore.Adapter.Events {
    /// <summary>
    /// Defines a subscription for receiving event messages for specific topics as push 
    /// notifications.
    /// </summary>
    public interface IEventMessageSubscriptionWithTopics : IAdapterSubscriptionWithTopics<EventMessage> { }
}
