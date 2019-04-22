using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Defines properties that can be used to identify a real-time data tag.
    /// </summary>
    public interface ITagIdentifier {

        /// <summary>
        /// The unique identifier for the tag.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The tag name.
        /// </summary>
        string Name { get; }

    }
}
