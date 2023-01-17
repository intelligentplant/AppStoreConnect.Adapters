using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an update to a push subscription.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubscriptionUpdateAction {

        /// <summary>
        /// A subscription is being added.
        /// </summary>
        Subscribe,

        /// <summary>
        /// A subscription is being removed.
        /// </summary>
        Unsubscribe

    }
}
