using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a proxy for an <see cref="IAdapter"/> that is running in an external system.
    /// </summary>
    public interface IAdapterProxy : IAdapter {

        /// <summary>
        /// Gets the descriptor for the remote adapter.
        /// </summary>
        AdapterDescriptor RemoteDescriptor { get; }

    }

}
