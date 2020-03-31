using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace DataCore.Adapter.Tests {
    public class ExampleCallContext : DefaultAdapterCallContext {

        private ExampleCallContext(ClaimsPrincipal user) : base(user) { }


        public static ExampleCallContext ForPrincipal(ClaimsPrincipal principal) {
            return new ExampleCallContext(principal);
        }


        public static ExampleCallContext ForIdentity(IIdentity user) {
            return new ExampleCallContext(user == null
                ? null
                : new ClaimsPrincipal(user));
        }

    }
}
