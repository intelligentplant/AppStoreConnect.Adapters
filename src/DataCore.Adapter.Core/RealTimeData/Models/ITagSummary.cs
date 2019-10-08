using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Defines properties that provide a high-level summary about a tag.
    /// </summary>
    public interface ITagSummary : ITagIdentifier {

        /// <summary>
        /// The tag measurement category (temperature, pressure, mass, etc).
        /// </summary>
        string Category { get; }

        /// <summary>
        /// The tag description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The tag units.
        /// </summary>
        string Units { get; }

    }
}
