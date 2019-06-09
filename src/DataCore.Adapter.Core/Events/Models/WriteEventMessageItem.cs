using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes an event message being written to an adapter.
    /// </summary>
    public class WriteEventMessageItem {

        /// <summary>
        /// The optional correlation ID for the operation.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// The event message to write.
        /// </summary>
        [Required]
        public EventMessage EventMessage { get; set; }

    }
}
