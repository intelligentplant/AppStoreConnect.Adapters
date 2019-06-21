using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub that returns information about the host and the available adapters.
    /// </summary>
    public class InfoHub : AdapterHubBase {

        /// <summary>
        /// Creates a new <see cref="InfoHub"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The host information.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        public InfoHub(HostInfo hostInfo, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor)
            : base(hostInfo, adapterCallContext, adapterAccessor) { }

    }
}
