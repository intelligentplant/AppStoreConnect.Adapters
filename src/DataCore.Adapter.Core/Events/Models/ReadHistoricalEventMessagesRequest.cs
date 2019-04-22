using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Base class for adapter requests related to reading historical event messages.
    /// </summary>
    public abstract class ReadHistoricalEventMessagesRequest : AdapterRequest {

        /// <summary>
        /// The event read direction. When <see cref="EventReadDirection.Backwards"/> is specified, 
        /// the resulting events will be returned in descending order of time.
        /// </summary>
        public EventReadDirection Direction { get; set; }

        /// <summary>
        /// The maximum number of event messages to retrieve. A value of less than one is interpreted 
        /// as meaning no limit. The adapter that handles the request can apply its own maximum limit 
        /// to the query.
        /// </summary>
        [Required]
        [DefaultValue(500)]
        public int MessageCount { get; set; }

    }
}
