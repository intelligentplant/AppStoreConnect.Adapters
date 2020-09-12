using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes a request to retrieve historical event messages using a cursor to specified the query start position.
    /// </summary>
    public class ReadEventMessagesUsingCursorRequest : ReadHistoricalEventMessagesRequest {

        /// <summary>
        /// The topic to read messages for. This property will be ignored if the adapter does not 
        /// support a topic-based event model.
        /// </summary>
        public string? Topic { get; set; }

        /// <summary>
        /// The cursor position to start the query at.
        /// </summary>
        public string CursorPosition { get; set; } = default!;

        /// <summary>
        /// The page size for the query.
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

    }
}
