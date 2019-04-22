using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Specifies the type of an annotation.
    /// </summary>
    public enum AnnotationType {

        /// <summary>
        /// The annotation is instantaneous, and applies to a single point in time.
        /// </summary>
        Instantaneous,

        /// <summary>
        /// The annotation applies to a time range, rather than a single point in time.
        /// </summary>
        TimeRange

    }
}
