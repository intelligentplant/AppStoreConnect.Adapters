using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IDataCoreContext"/> implementation for ASP.NET Core.
    /// </summary>
    public class DataCoreContext: IDataCoreContext {

        /// <summary>
        /// The <see cref="HttpContext"/> associated with the <see cref="DataCoreContext"/>.
        /// </summary>
        private readonly HttpContext _httpContext;


        /// <inheritdoc/>
        public ClaimsPrincipal User {
            get { return _httpContext.User; }
        }


        /// <summary>
        /// Creates a new <see cref="DataCoreContext"/> object.
        /// </summary>
        /// <param name="httpContextAccessor">
        ///   The <see cref="IHttpContextAccessor"/> service.
        /// </param>
        public DataCoreContext(IHttpContextAccessor httpContextAccessor) {
            _httpContext = httpContextAccessor?.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }


        /// <inheritdoc/>
        public object GetService(Type serviceType) {
            return _httpContext.RequestServices?.GetService(serviceType);
        }
    }
}
