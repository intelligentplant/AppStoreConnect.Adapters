using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// A descriptor for an App Store Connect adapter.
    /// </summary>
    public interface IAdapterDescriptor {

        /// <summary>
        /// The identifier for the adapter. This can be any type of value, as long as it is unique 
        /// within the hosting application, and does not change.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The adapter name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        string Description { get; }

    }
}
