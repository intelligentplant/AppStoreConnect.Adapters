using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Http.Proxy {

    /// <summary>
    /// SignalR options for an <see cref="HttpAdapterProxy"/>.
    /// </summary>
    public class SignalROptions {

        /// <summary>
        /// A delegate that is used to uniquely identify a SignalR connection for a given 
        /// <see cref="IAdapterCallContext"/>.
        /// </summary>
        public ConnectionIdentityFactory? ConnectionIdentityFactory { get; set; }

        /// <summary>
        /// A delegate that can be used to create SignalR connections for features that require 
        /// long-running subscriptions.
        /// </summary>
        [Required]
        public ConnectionFactory ConnectionFactory { get; set; } = default!;

        /// <summary>
        /// The time-to-live for a SignalR connection when there are no active streams ongoing.
        /// </summary>
        public TimeSpan TimeToLive { get; set; } = TimeSpan.FromSeconds(30);

    }
}
