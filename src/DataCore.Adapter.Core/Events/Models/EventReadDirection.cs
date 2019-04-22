using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes the read direction for a historical event message read operation.
    /// </summary>
    public enum EventReadDirection {

        /// <summary>
        /// Read forwards from the query start time or cursor position.
        /// </summary>
        Forwards,

        /// <summary>
        /// Read backwards from the query end time or cursor position.
        /// </summary>
        Backwards

    }
}
