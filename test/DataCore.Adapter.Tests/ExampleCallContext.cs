using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace DataCore.Adapter.Tests {
    public class ExampleCallContext : IAdapterCallContext {

        public ClaimsPrincipal User { get; }

        public string ConnectionId { get; } = Guid.NewGuid().ToString();

        public string CorrelationId { get; } = Guid.NewGuid().ToString();

        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();


        private ExampleCallContext(ClaimsPrincipal user) {
            User = user;
        }


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
