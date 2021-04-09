using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes a request to write event messages to an adapter.
    /// </summary>
    public class WriteEventMessagesRequestExtended : WriteEventMessagesRequest {

        /// <summary>
        /// The event messages to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public WriteEventMessageItem[] Events { get; set; } = Array.Empty<WriteEventMessageItem>();

    }
}
