using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation that uses a SignalR 
    /// <see cref="HubCallerContext"/> to provide context settings.
    /// </summary>
    public class SignalRAdapterCallContext : IAdapterCallContext {

        /// <summary>
        /// The SignalR hub caller context.
        /// </summary>
        private readonly HubCallerContext _hubCallerContext;

        /// <inheritdoc/>
        public ClaimsPrincipal User {
            get { return _hubCallerContext.User; }
        }

        /// <inheritdoc/>
        public string ConnectionId {
            get { return _hubCallerContext.ConnectionId; }
        }

        /// <inheritdoc/>
        public string CorrelationId {
            get { return null; }
        }

        /// <inheritdoc/>
        public CultureInfo CultureInfo {
            get { return _hubCallerContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture; }
        }

        /// <inheritdoc/>
        public IDictionary<object, object> Items {
            get { return _hubCallerContext.Items; }
        }


        /// <summary>
        /// Creates a new <see cref="SignalRAdapterCallContext"/> object.
        /// </summary>
        /// <param name="hubCallerContext">
        ///   The hub caller context.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hubCallerContext"/> is <see langword="null"/>.
        /// </exception>
        public SignalRAdapterCallContext(HubCallerContext hubCallerContext) {
            _hubCallerContext = hubCallerContext ?? throw new ArgumentNullException(nameof(hubCallerContext));
        }

    }
}
