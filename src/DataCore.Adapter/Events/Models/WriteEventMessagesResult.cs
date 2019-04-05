using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes the result of an event message write operation.
    /// </summary>
    public sealed class WriteEventMessagesResult {

        /// <summary>
        /// Indicates if the write was successful.
        /// </summary>
        public bool Success { get; }


        /// <summary>
        /// Creates a new <see cref="WriteEventMessagesResult"/> object.
        /// </summary>
        /// <param name="success">
        ///   A flag indicating if the write was successful.
        /// </param>
        public WriteEventMessagesResult(bool success) {
            Success = success;
        }

    }
}
