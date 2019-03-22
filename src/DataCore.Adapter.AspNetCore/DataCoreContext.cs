using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DataCore.Adapter.AspNetCore {
    public class DataCoreContext: IDataCoreContext {

        private readonly HttpContext _httpContext;


        public ClaimsPrincipal User {
            get { return _httpContext.User; }
        }


        public DataCoreContext(IHttpContextAccessor httpContextAccessor) {
            _httpContext = httpContextAccessor?.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }


        public object GetService(Type serviceType) {
            return _httpContext.RequestServices?.GetService(serviceType);
        }
    }
}
