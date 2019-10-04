using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a proxy for an <see cref="IAdapter"/> that is running in an external system.
    /// </summary>
    public interface IAdapterProxy : IAdapter {

        /// <summary>
        /// Gets information about the remote host.
        /// </summary>
        HostInfo RemoteHostInfo { get; }

        /// <summary>
        /// Gets the descriptor for the remote adapter.
        /// </summary>
        AdapterDescriptorExtended RemoteDescriptor { get; }

    }

}
