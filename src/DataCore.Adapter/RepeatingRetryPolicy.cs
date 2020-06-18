using System;
using System.Linq;

namespace DataCore.Adapter {

    /// <summary>
    /// Retry policy that emits a fixed set of retry delays and then repeats the final delay for 
    /// subsequent attempts.
    /// </summary>
    public sealed class RepeatingRetryPolicy : IRetryPolicy {

        /// <summary>
        /// The retry delays to use.
        /// </summary>
        private readonly TimeSpan?[] _retryDelays;


        /// <summary>
        /// Creates a new <see cref="RepeatingRetryPolicy"/> that will use retry delays of 0, 2, 10, 
        /// and 30 seconds.
        /// </summary>
        public RepeatingRetryPolicy() {
            _retryDelays = DefaultRetryPolicy.DefaultRetryDelays;
        }


        /// <summary>
        /// Creates a new <see cref="RepeatingRetryPolicy"/> that will use the specified retry delays.
        /// </summary>
        /// <param name="retryDelays">
        ///   The retry delays.
        /// </param>
        public RepeatingRetryPolicy(params TimeSpan[] retryDelays) {
            _retryDelays = retryDelays.Cast<TimeSpan?>().ToArray();
            if (_retryDelays.Length == 0) {
                _retryDelays = DefaultRetryDelays;
            }
        }


        /// <inheritdoc/>
        public TimeSpan? NextRetryDelay(RetryContext retryContext) {
            if (retryContext == null) {
                throw new ArgumentNullException(nameof(retryContext));
            }

            if (retryContext.PreviousRetryCount >= _retryDelays.Length) {
                return _retryDelays[_retryDelays.Length - 1];
            }

            return _retryDelays[retryContext.PreviousRetryCount];
        }

    }
}
