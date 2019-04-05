using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes the status of a write operation.
    /// </summary>
    public enum WriteStatus {

        /// <summary>
        /// Write status is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The write was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The write was unsuccessful.
        /// </summary>
        Fail,

        /// <summary>
        /// The write is pending (for example, it may have been added to a processing queue or 
        /// scheduled for later).
        /// </summary>
        Pending

    }
}
