using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes a request to retrieve historical event messages using a cursor to specify the query start position.
    /// </summary>
    public class ReadEventMessagesUsingCursorRequest : ReadHistoricalEventMessagesRequest {

        /// <summary>
        /// The topic to read messages for. This property will be ignored if the adapter does not 
        /// support a topic-based event model.
        /// </summary>
        [MaxLength(500)]
        public string? Topic { get; set; }

        /// <summary>
        /// The cursor position to start the query at.
        /// </summary>
        [MaxLength(500)]
        public string? CursorPosition { get; set; }

        /// <summary>
        /// The page size for the query.
        /// </summary>
        [Range(1, 500)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

    }
}
