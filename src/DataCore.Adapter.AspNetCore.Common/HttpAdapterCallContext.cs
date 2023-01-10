using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation that uses an <see cref="HttpContext"/> to 
    /// provide context settings.
    /// </summary>
    public class HttpAdapterCallContext : IAdapterCallContext {

        /// <summary>
        /// The <see cref="HttpContext"/> associated with the <see cref="HttpAdapterCallContext"/>.
        /// </summary>
        private readonly HttpContext _httpContext;

        /// <inheritdoc/>
        public ClaimsPrincipal? User {
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
            get { return _httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture ?? CultureInfo.CurrentCulture; }
        }

        /// <inheritdoc/>
        public IDictionary<object, object?> Items {
            get { return _httpContext.Items; }
        }


        /// <summary>
        /// Creates a new <see cref="HttpAdapterCallContext"/> object.
        /// </summary>
        /// <param name="httpContext">
        ///   The <see cref="HttpContext"/> to use.
        /// </param>
        /// <param name="validateRequests">
        ///   Specifies if adapters need to validate request objects when adapter features are 
        ///   invoked using this call context. Specify <see langword="false"/> if the request 
        ///   objects have already been validated by the route handler for the 
        ///   <paramref name="httpContext"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="httpContext"/> is <see langword="null"/>
        /// </exception>
        public HttpAdapterCallContext(HttpContext httpContext, bool validateRequests = false) {
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            if (!validateRequests) {
                this.UseRequestValidation(false);
            }
        }

    }
}
