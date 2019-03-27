using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation for ASP.NET Core.
    /// </summary>
    public class AdapterCallContext: IAdapterCallContext {

        /// <summary>
        /// The <see cref="HttpContext"/> associated with the <see cref="AdapterCallContext"/>.
        /// </summary>
        private readonly HttpContext _httpContext;


        /// <inheritdoc/>
        public ClaimsPrincipal User {
            get { return _httpContext.User; }
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


        /// <inheritdoc/>
        public object GetService(Type serviceType) {
            return _httpContext.RequestServices?.GetService(serviceType);
        }
    }
}
