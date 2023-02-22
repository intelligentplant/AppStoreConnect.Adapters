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
    public class HttpAdapterCallContext : IAdapterCallContext<HttpContext> {

        /// <inheritdoc/>
        public HttpContext Provider { get; }

        /// <inheritdoc/>
        public ClaimsPrincipal? User {
            get { return Provider.User; }
        }

        /// <inheritdoc/>
        public string ConnectionId {
            get { return Provider.Connection.Id; }
        }

        /// <inheritdoc/>
        public string CorrelationId {
            get { return Provider.TraceIdentifier; }
        }

        /// <inheritdoc/>
        public CultureInfo CultureInfo {
            get { return Provider.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture ?? CultureInfo.CurrentCulture; }
        }

        /// <inheritdoc/>
        public IDictionary<object, object?> Items {
            get { return Provider.Items; }
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
            Provider = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        }

    }
}
