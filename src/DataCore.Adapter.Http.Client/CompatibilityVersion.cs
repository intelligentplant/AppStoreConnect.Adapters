namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Describes the App Store Connect adapter toolkit version that the client should use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Following convension used by ASP.NET Core MVC")]
    public enum CompatibilityVersion {
        /// <summary>
        /// v1.0
        /// </summary>
        Version_1_0 = 1,
        /// <summary>
        /// v2.0
        /// </summary>
        Version_2_0 = 2,
        /// <summary>
        /// v3.0
        /// </summary>
        Version_3_0 = 3,
        /// <summary>
        /// The current version.
        /// </summary>
        Latest = int.MaxValue
    }
}
