using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Configuration {

    /// <summary>
    /// Describes a validation error while parsing adapter options.
    /// </summary>
    public class ValidationError {

        /// <summary>
        /// The message describing the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The line number indicating where the error occurred.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The line position indicating where the error occurred.
        /// </summary>
        public int LinePosition { get; set; }

    }
}
