using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation that uses an <see cref="Microsoft.AspNetCore.Http.HttpContext"/> to 
    /// provide context settings.
    /// </summary>
    public class HttpAdapterCallContext : IAdapterCallContext {

        /// <summary>
        /// The <see cref="Microsoft.AspNetCore.Http.HttpContext"/> associated with the <see cref="HttpAdapterCallContext"/>.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <inheritdoc/>
        public ClaimsPrincipal? User {
            get { return HttpContext.User; }
        }

        /// <inheritdoc/>
        public string ConnectionId {
            get { return HttpContext.Connection.Id; }
        }

        /// <inheritdoc/>
        public string CorrelationId {
            get { return HttpContext.TraceIdentifier; }
        }

        /// <inheritdoc/>
        public CultureInfo CultureInfo {
            get { return HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture ?? CultureInfo.CurrentCulture; }
        }

        /// <inheritdoc/>
        public IDictionary<object, object?> Items {
            get { return HttpContext.Items; }
        }


        /// <summary>
        /// Creates a new <see cref="HttpAdapterCallContext"/> object.
        /// </summary>
        /// <param name="httpContext">
        ///   The <see cref="Microsoft.AspNetCore.Http.HttpContext"/> to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="httpContext"/> is <see langword="null"/>
        /// </exception>
        public HttpAdapterCallContext(HttpContext httpContext) {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        }

    }
}
