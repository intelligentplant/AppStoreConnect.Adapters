using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes a request to write event messages to an adapter.
    /// </summary>
    public class WriteEventMessagesRequest {

        /// <summary>
        /// The event messages to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public IEnumerable<WriteEventMessageItem> Events { get; set; }

    }
}
