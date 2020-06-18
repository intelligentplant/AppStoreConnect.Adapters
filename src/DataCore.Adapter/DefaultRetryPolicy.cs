using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter {
    public sealed class DefaultRetryPolicy : IRetryPolicy {

        internal static readonly TimeSpan?[] DefaultRetryDelays = { 
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            null
        };

        private readonly TimeSpan?[] _retryDelays;


        public DefaultRetryPolicy() {
            _retryDelays = DefaultRetryDelays;
        }


        public DefaultRetryPolicy(params TimeSpan[] retryDelays) {
            _retryDelays = retryDelays.Cast<TimeSpan?>().ToArray();
        }


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
