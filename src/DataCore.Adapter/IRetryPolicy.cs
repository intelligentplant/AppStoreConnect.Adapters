using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a policy for retrying an action in the event of failure.
    /// </summary>
    public interface IRetryPolicy {

        /// <summary>
        /// Gets the delay to use before retrying the action again.
        /// </summary>
        /// <param name="retryContext">
        ///   The retry context, describing the previous retry attempt (if any).
        /// </param>
        /// <returns>
        ///   The delay to use before retrying the action. If the return value is <see langword="null"/>, 
        ///   no further retry attempts will be made.
        /// </returns>
        TimeSpan? NextRetryDelay(RetryContext retryContext);

    }
}
