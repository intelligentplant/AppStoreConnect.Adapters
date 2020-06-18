using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Retry context passed to <see cref="IRetryPolicy.NextRetryDelay(RetryContext)"/>.
    /// </summary>
    public sealed class RetryContext {

        /// <summary>
        /// The number of consecutive failed retries so far.
        /// </summary>
        public long PreviousRetryCount { get; set; }

        /// <summary>
        /// The amount of time spent retrying so far.
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// The error that caused the current retry. Can be <see langword="null"/>.
        /// </summary>
        public Exception RetryReason { get; set; }

    }

}
