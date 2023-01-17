using System.Text.Json.Serialization;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes the type of an event message push subscription.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventMessageSubscriptionType {

        /// <summary>
        /// The subscription is active. Adapters can require that at least one active subscription 
        /// is created before they start emitting event messages.
        /// </summary>
        Active,

        /// <summary>
        /// The subscription is passive. Emitted event messages will be sent to the subscriber, 
        /// but the adapter may require that at least one active subscription is created before it 
        /// starts to emit event messages.
        /// </summary>
        Passive

    }
}
