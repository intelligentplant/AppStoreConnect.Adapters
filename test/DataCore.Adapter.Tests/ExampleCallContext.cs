using System;
using System.Security.Claims;
using System.Security.Principal;

namespace DataCore.Adapter.Tests {
    public class ExampleCallContext : DefaultAdapterCallContext {

        private ExampleCallContext(ClaimsPrincipal user, IServiceProvider serviceProvider) : base(user, serviceProvider: serviceProvider) { }


        public static ExampleCallContext ForPrincipal(ClaimsPrincipal principal, IServiceProvider serviceProvider = null) {
            return new ExampleCallContext(principal, serviceProvider);
        }


        public static ExampleCallContext ForIdentity(IIdentity user, IServiceProvider serviceProvider = null) {
            return new ExampleCallContext(user == null
                ? null
                : new ClaimsPrincipal(user), serviceProvider);
        }

    }
}
