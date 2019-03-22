
using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes the quality status of a tag value.
    /// </summary>
    public enum TagValueStatus {

        /// <summary>
        /// The quality of the value is bad. This could indicate, for example, an instrument failure.
        /// </summary>
        Bad = 0,

        /// <summary>
        /// The quality of the value is unknown.
        /// </summary>
        Unknown = 64,

        /// <summary>
        /// The quality of the status is good.
        /// </summary>
        Good = 192

    }
}
