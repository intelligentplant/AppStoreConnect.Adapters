using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes a request to write event messages to an adapter.
    /// </summary>
    public class WriteEventMessagesRequest {

        /// <summary>
        /// The event messages to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public WriteEventMessageItem[] Events { get; set; }

    }
}
