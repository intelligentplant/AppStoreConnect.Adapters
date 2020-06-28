using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// A request to create an event message subscription.
    /// </summary>
    public class CreateEventMessageSubscriptionRequest : AdapterRequest {

        /// <summary>
        /// Specifies if the adapter should treat this as an active or passive subscription. Some 
        /// adapters will only emit event messages when they have at least one active subscriber.
        /// </summary>
        public EventMessageSubscriptionType SubscriptionType { get; set; }

    }

}
