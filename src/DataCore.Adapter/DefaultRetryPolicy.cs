using System;
using System.Linq;

namespace DataCore.Adapter {

    /// <summary>
    /// Retry policy that emits a fixed set of retry delays and then indicates that no more retry 
    /// attempts should be made.
    /// </summary>
    public sealed class DefaultRetryPolicy : IRetryPolicy {

        /// <summary>
        /// The default retry delays to use.
        /// </summary>
        internal static readonly TimeSpan?[] DefaultRetryDelays = { 
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// The retry delays to use.
        /// </summary>
        private readonly TimeSpan?[] _retryDelays;


        /// <summary>
        /// Creates a new <see cref="DefaultRetryPolicy"/> that will use retry delays of 0, 2, 10, 
        /// and 30 seconds.
        /// </summary>
        public DefaultRetryPolicy() {
            _retryDelays = DefaultRetryDelays;
        }


        /// <summary>
        /// Creates a new <see cref="DefaultRetryPolicy"/> that will use the specified retry delays.
        /// </summary>
        /// <param name="retryDelays">
        ///   The retry delays.
        /// </param>
        public DefaultRetryPolicy(params TimeSpan[] retryDelays) {
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
                return null;
            }

            return _retryDelays[retryContext.PreviousRetryCount];
        }
    }
}
