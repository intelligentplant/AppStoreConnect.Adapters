namespace DataCore.Adapter.Common {

    /// <summary>
    /// Constant values that can be used when creating <see cref="StatusCode"/> instances.
    /// </summary>
    public static class StatusCodes {

        /// <summary>
        /// Good quality (non-specific).
        /// </summary>
        public const uint Good = 0x00000000;

        /// <summary>
        /// Uncertain quality (non-specific).
        /// </summary>
        public const uint Uncertain = 0x40000000;

        /// <summary>
        /// Bad quality (non-specific).
        /// </summary>
        public const uint Bad = 0x80000000;

    }
}
