using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client {

    /// <summary>
    /// <see cref="IRetryPolicy"/> implementation that allows a set of retry intervals to be 
    /// specified, and keeps repeating the final interval if required until reconnection occurs or 
    /// the <see cref="RepeatingRetryPolicy"/> is disposed.
    /// </summary>
    /// <example>
    /// Creates a <see cref="RepeatingRetryPolicy"/> object that uses a retry delay of 5 seconds, 
    /// and then subsequently 30 seconds, until reconnection occurs.
    /// <code lang="C#">
    /// var retryPolicy = new RepeatingRetryPolicy(5000, 30000);
    /// </code>
    /// </example>
    public sealed class RepeatingRetryPolicy : IRetryPolicy, IDisposable {

        /// <summary>
        /// Flags if the policy has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The set of retry intervals to apply.
        /// </summary>
        private readonly int[] _retryIntervals;

        /// <summary>
        /// Creates a new <see cref="RepeatingRetryPolicy"/> with the specified retry intervals.
        /// </summary>
        /// <param name="retryIntervals">
        ///   The retry intervals, specified in milliseconds. Values less than zero are ignored.
        /// </param>
        public RepeatingRetryPolicy(params int[] retryIntervals)
            : this((IEnumerable<int>) retryIntervals) { }


        /// <summary>
        /// Creates a new <see cref="RepeatingRetryPolicy"/> with the specified retry intervals.
        /// </summary>
        /// <param name="retryIntervals">
        ///   The retry intervals, specified in milliseconds. Values less than zero are ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="retryIntervals"/> is <see langword="null"/>.
        /// </exception>
        public RepeatingRetryPolicy(IEnumerable<int> retryIntervals) {
            if (retryIntervals == null) {
                throw new ArgumentNullException(nameof(retryIntervals));
            }
            _retryIntervals = retryIntervals.Where(x => x >= 0).ToArray();
        }


        /// <inheritdoc/>
        public TimeSpan? NextRetryDelay(RetryContext retryContext) {
            if (_isDisposed || retryContext == null || _retryIntervals.Length == 0) {
                return null;
            }

            if (retryContext.PreviousRetryCount >= _retryIntervals.Length) {
                return TimeSpan.FromMilliseconds(_retryIntervals[_retryIntervals.Length - 1]);
            }

            return TimeSpan.FromMilliseconds(_retryIntervals[retryContext.PreviousRetryCount]);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
        }
    }
}
