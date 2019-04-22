using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes a request to retrieve historical event messages using a cursor to specified the query start position.
    /// </summary>
    public class ReadEventMessagesUsingCursorRequest : ReadHistoricalEventMessagesRequest {

        /// <summary>
        /// The cursor position to start the query at.
        /// </summary>
        public string Cursor { get; set; }

    }
}
