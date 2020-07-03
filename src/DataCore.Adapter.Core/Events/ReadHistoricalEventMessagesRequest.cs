using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base class for adapter requests related to reading historical event messages.
    /// </summary>
    public abstract class ReadHistoricalEventMessagesRequest : AdapterRequest {

        /// <summary>
        /// The event read direction. When <see cref="EventReadDirection.Backwards"/> is specified, 
        /// the resulting events will be returned in descending order of time.
        /// </summary>
        public EventReadDirection Direction { get; set; }

    }
}
