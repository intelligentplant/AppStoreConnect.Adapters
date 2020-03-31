using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation for ASP.NET Core.
    /// </summary>
    public class AdapterCallContext : IAdapterCallContext {

        /// <summary>
        /// The <see cref="HttpContext"/> associated with the <see cref="AdapterCallContext"/>.
        /// </summary>
        private readonly HttpContext _httpContext;

        /// <inheritdoc/>
        public ClaimsPrincipal User {
            get { return _httpContext.User; }
        }

        /// <inheritdoc/>
        public string ConnectionId {
            get { return _httpContext.Connection.Id; }
        }

        /// <inheritdoc/>
        public string CorrelationId {
            get { return _httpContext.TraceIdentifier; }
        }

        /// <inheritdoc/>
        public CultureInfo CultureInfo {
            get { return _httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture; }
        }

        /// <inheritdoc/>
        public IDictionary<object, object> Items {
            get { return _httpContext.Items; }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterCallContext"/> object.
        /// </summary>
        /// <param name="httpContextAccessor">
        ///   The <see cref="IHttpContextAccessor"/> service.
        /// </param>
        public AdapterCallContext(IHttpContextAccessor httpContextAccessor) {
            _httpContext = httpContextAccessor?.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

    }
}
