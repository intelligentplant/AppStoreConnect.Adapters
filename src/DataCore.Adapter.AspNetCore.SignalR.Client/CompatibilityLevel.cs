namespace DataCore.Adapter.AspNetCore.SignalR.Client {

    /// <summary>
    /// Describes the compatibility level of the SignalR server that a <see cref="AdapterSignalRClient"/> 
    /// is connecting to.
    /// </summary>
    public enum CompatibilityLevel {
        /// <summary>
        /// The host application is running ASP.NET Core 3.x or later.
        /// </summary>
        AspNetCore3,
        /// <summary>
        /// The host application is running ASP.NET Core 2.x.
        /// </summary>
        AspNetCore2,
        /// <summary>
        /// The host application is running the latest supported version of ASP.NET Core.
        /// </summary>
        Latest = AspNetCore3,
    }
}
