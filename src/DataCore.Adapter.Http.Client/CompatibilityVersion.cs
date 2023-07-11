namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Describes the App Store Connect adapter toolkit version that the client should use.
    /// </summary>
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
