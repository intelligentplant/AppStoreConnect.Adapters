using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// A request to create an event subscription for a specific event topic.
    /// </summary>
    public class CreateEventMessageTopicSubscriptionRequest : CreateEventMessageSubscriptionRequest {

        /// <summary>
        /// The topic name.
        /// </summary>
        [Required]
        public string Topic { get; set; }

    }
}
